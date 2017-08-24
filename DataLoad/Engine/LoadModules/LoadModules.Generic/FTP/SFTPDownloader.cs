﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using CatalogueLibrary;
using DataLoadEngine.DataProvider;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using ReusableLibraryCode.Progress;

namespace LoadModules.Generic.FTP
{
    [Description(
        "Operates in the same way as it's parent except that it uses SSH via the API Tamir.SharpSsh.  In addition this class will not bother downloading any files that already exist in the forLoading directory (have the same name - file size is NOT checked)")]
    public class SFTPDownloader:FTPDownloader
    {
        
        protected override void Download(string file, IHICProjectDirectory destination,IDataLoadEventListener job)
        {
            if (file.Contains("/") || file.Contains("\\"))
                throw new Exception("Was not expecting a relative path here");
            
            Stopwatch s = new Stopwatch();
            s.Start();
            
            using(var sftp = new SftpClient(_host,_username,_password))
            {
                sftp.ConnectionInfo.Timeout = new TimeSpan(0, 0, 0, TimeoutInSeconds);
                sftp.Connect();
                
                //if there is a specified remote directory then reference it otherwise reference it locally (or however we were told about it from GetFileList())
                string fullFilePath = !string.IsNullOrWhiteSpace(_remoteDir) ? Path.Combine(_remoteDir, file) : file;
                
                string destinationFilePath = Path.Combine(destination.ForLoading.FullName, file);

                //register for events
                Action<ulong> callback = (totalBytes) => job.OnProgress(this, new ProgressEventArgs(destinationFilePath, new ProgressMeasurement((int)(totalBytes * 0.001), ProgressType.Kilobytes), s.Elapsed));

                using (var fs = new FileStream(destinationFilePath, FileMode.CreateNew))
                {
                    //download
                    sftp.DownloadFile(fullFilePath, fs, callback);
                    fs.Close();
                }
                _filesRetrieved.Add(fullFilePath);

            }
            s.Stop();
        }


        public override void LoadCompletedSoDispose(ExitCodeType exitCode, IDataLoadEventListener postLoadEventListener)
        {
            if(exitCode == ExitCodeType.Success)
            {
                using (var sftp = new SftpClient(_host, _username, _password))
                {
                    sftp.ConnectionInfo.Timeout = new TimeSpan(0, 0, 0, TimeoutInSeconds);
                    sftp.Connect();
                    
                    foreach (string retrievedFiles in _filesRetrieved)
                        try
                        {
                            sftp.DeleteFile(retrievedFiles);
                            postLoadEventListener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Deleted SFTP file " + retrievedFiles + " from SFTP server"));
                        }
                        catch (Exception e)
                        {
                            postLoadEventListener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Could not delete SFTP file " + retrievedFiles + " from SFTP server", e));
                        }
                }
                
            }
        }


        protected override string[] GetFileList()
        {
            using (var sftp = new SftpClient(_host, _username, _password))
            {
                sftp.ConnectionInfo.Timeout = new TimeSpan(0, 0, 0, TimeoutInSeconds);
                sftp.Connect();

                string directory = _remoteDir;

                if (string.IsNullOrWhiteSpace(directory))
                    directory = ".";
                
                return sftp.ListDirectory(directory).Select(d=>d.Name).ToArray();
            }

        }
    }
}