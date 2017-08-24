﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using CatalogueLibrary;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.DataFlowPipeline;
using DataLoadEngine.DataProvider;
using DataLoadEngine.Job;
using Microsoft.SqlServer.Management.Smo;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
using ReusableLibraryCode.Progress;

namespace LoadModules.Generic.FTP
{
    [Description(

        @"Checks the HICProjectDirectory for an FTP configuration file (ftp_details.xml).  It then attempts to connect to the FTP server and download all files in the landing folder of the FTP (make sure you really want everything in the root folder - if not then configure redirection on the FTP so you land in the correct directory).  Files are downloaded into the ForLoading folder")]
    public class FTPDownloader : IPluginDataProvider
    {
        protected string _host;
        protected string _port;
        protected string _username;
        protected string _password;
        protected string _remoteDir;

        private bool _useSSL = false;

        protected List<string> _filesRetrieved = new List<string>();
        private IHICProjectDirectory _directory;

        [DemandsInitialization("Determines the behaviour of the system when no files are found on the server.  If true the entire data load process immediately stops with exit code LoadNotRequired, if false then the load proceeds as normal (useful if for example if you have multiple Attachers and some files are optional)")]
        public bool SendLoadNotRequiredIfFileNotFound { get; set; }
        
        [DemandsInitialization("The Regex expression to validate files on the FTP server against, only files matching the expression will be downloaded")]
        public Regex FilePattern { get; set; }

        [DemandsInitialization("The timeout to use when connecting to the FTP server in SECONDS")]
        public int TimeoutInSeconds { get; set; }

        [DemandsInitialization("Tick to delete files from the FTP server when the load is succesful (ends with .Success not .OperationNotRequired - which happens when LoadNotRequired state).  This will only delete the files if they were actually fetched from the FTP server.  If the files were already in forLoading then the remote files are not deleted")]
        public bool DeleteFilesOffFTPServerAfterSuccesfulDataLoad { get; set; }

        public void Initialize(IHICProjectDirectory hicProjectDirectory, DiscoveredDatabase dbInfo)
        {
            _directory = hicProjectDirectory;
        }

        public ExitCodeType Fetch(IDataLoadJob job, GracefulCancellationToken cancellationToken)
        {
            SetupFTP(_directory);
            return DownloadFilesOnFTP(_directory, job);
        }

        public string GetDescription()
        {
            return "See Description attribute of class";
        }

        public IDataProvider Clone()
        {
            return new FTPDownloader();
        }

        public bool Validate(IHICProjectDirectory destination)
        {
            SetupFTP(destination);
            return GetFileList().Any();
        }

        private void SetupFTP(IHICProjectDirectory destination)
        {
            if (destination.FTPDetails == null)
                throw new NullReferenceException("Destination.FTPDetails - provided by "+typeof(HICProjectDirectory).FullName+" class was null");

            XmlDocument doc = new XmlDocument();
            doc.Load(destination.FTPDetails.FullName);


            _host = GetValueOfTag(doc, "Host");
            _port = GetValueOfTag(doc, "Port");
            _username = GetValueOfTag(doc, "User");
            _password = GetValueOfTag(doc, "Pass");
            _remoteDir = GetValueOfTag(doc, "RemoteDir");

            if(string.IsNullOrWhiteSpace(_host) || string.IsNullOrWhiteSpace(_username) || string.IsNullOrWhiteSpace(_password))
                throw new NullReferenceException("Missing FTP details from file " + destination.FTPDetails.FullName);

        }

        private ExitCodeType DownloadFilesOnFTP(IHICProjectDirectory destination, IDataLoadEventListener listener)
        {
            string[] files = GetFileList();

            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, files.Aggregate("Identified the following files on the FTP server:", (s, f) => f + ",").TrimEnd(',')));
            
            bool forLoadingContainedCachedFiles = false;

            foreach (string file in files)
            {
                var action = GetSkipActionForFile(file, destination);

                listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information, "File " + file + " was evaluated as " + action));
                if(action == SkipReason.DoNotSkip)
                {
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "About to download "+file));
                    Download(file, destination, listener);
                }

                if (action == SkipReason.InForLoading)
                    forLoadingContainedCachedFiles = true;
            }

            //if no files were downloaded (and there were none skiped because they were in forLoading) and in that eventuality we have our flag set to return LoadNotRequired then do so
            if (!forLoadingContainedCachedFiles && !_filesRetrieved.Any() && SendLoadNotRequiredIfFileNotFound)
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Could not find any files on the remote server worth downloading, so returning LoadNotRequired"));
                return ExitCodeType.OperationNotRequired;
            }

            //otherwise it was a success - even if no files were actually retrieved... hey that's what the user said, otherwise he would have set SendLoadNotRequiredIfFileNotFound
            return ExitCodeType.Success;
        }

        protected enum SkipReason
        {
            DoNotSkip,
            InForLoading,
            DidNotMatchPattern,
            IsImaginaryFile
        }

        protected SkipReason GetSkipActionForFile(string file, IHICProjectDirectory destination)
        {
            if (file.StartsWith("."))
                return SkipReason.IsImaginaryFile;

            //if there is a regex pattern
            if(FilePattern != null)
                if (!FilePattern.IsMatch(file))//and it does not match
                    return SkipReason.DidNotMatchPattern; //skip because it did not match pattern

            //if the file on the FTP already exists in the forLoading directory, skip it
            if (destination.ForLoading.GetFiles(file).Any())
                return SkipReason.InForLoading;

         
            return SkipReason.DoNotSkip;
        }


        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
        {
            return true;//any cert will do! yay
        }


        private string GetValueOfTag(XmlDocument doc, string tagName)
        {
            XmlNodeList tagsFound = doc.GetElementsByTagName(tagName);
            if(tagsFound.Count != 1)
                throw new Exception("Found " + tagsFound.Count + " tags called " + tagName);

            if (string.IsNullOrWhiteSpace(tagsFound[0].Value)) //try for value
                return tagsFound[0].InnerText; //guess theres no value, so just do inner text... whatevs
            else
                return tagsFound[0].Value;
        }



        protected virtual string[] GetFileList()
        {
            StringBuilder result = new StringBuilder();
            WebResponse response = null;
            StreamReader reader = null;
            try
            {
                FtpWebRequest reqFTP;

                string uri;


                if (!string.IsNullOrWhiteSpace(_remoteDir))
                    uri = "ftp://" + _host + ":" + _port + "/" + _remoteDir;
                else
                    uri = "ftp://" + _host + ":" + _port;

                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(_username, _password);
                reqFTP.Method = WebRequestMethods.Ftp.ListDirectory;
                reqFTP.Timeout = TimeoutInSeconds*1000;

                reqFTP.Proxy = null;
                reqFTP.KeepAlive = false;
                reqFTP.UsePassive = true;
                reqFTP.EnableSsl = _useSSL;

                //accept any certificates
                ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
                response = reqFTP.GetResponse();

                reader = new StreamReader(response.GetResponseStream());
                string line = reader.ReadLine();
                while (line != null)
                {
                    result.Append(line);
                    result.Append("\n");
                    line = reader.ReadLine();
                }
                // to remove the trailing '\n'
                result.Remove(result.ToString().LastIndexOf('\n'), 1);
                return result.ToString().Split('\n');
            }
            finally
            {
                if (reader != null)
                    reader.Close();

                if (response != null)
                    response.Close();
            }
        }

        protected virtual void Download(string file, IHICProjectDirectory destination,IDataLoadEventListener job)
        {

            Stopwatch s = new Stopwatch();
            s.Start();

            string uri;
            if (!string.IsNullOrWhiteSpace(_remoteDir))
                uri = "ftp://" + _host + ":" + _port + "/" + _remoteDir + "/" + file;
            else
                uri = "ftp://" + _host + ":" + _port + "/" + file;

            if (_useSSL)
                uri = "s" + uri;

            Uri serverUri = new Uri(uri);
            if (serverUri.Scheme != Uri.UriSchemeFtp)
            {
                return;
            }

            FtpWebRequest reqFTP;
            reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));
            reqFTP.Credentials = new NetworkCredential(_username, _password);
            reqFTP.KeepAlive = false;
            reqFTP.Method = WebRequestMethods.Ftp.DownloadFile;
            reqFTP.UseBinary = true;
            reqFTP.Proxy = null;
            reqFTP.UsePassive = true;
            reqFTP.EnableSsl = _useSSL;
            reqFTP.Timeout = TimeoutInSeconds*1000;

            FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
            Stream responseStream = response.GetResponseStream();
            string destinationFileName = Path.Combine(destination.ForLoading.FullName, file);
            
            using (FileStream writeStream = new FileStream(destinationFileName, FileMode.Create))
            {
                int Length = 2048;
                Byte[] buffer = new Byte[Length];
                int bytesRead = responseStream.Read(buffer, 0, Length);
                int totalBytesReadSoFar = bytesRead;

                while (bytesRead > 0)
                {
                    writeStream.Write(buffer, 0, bytesRead);
                    bytesRead = responseStream.Read(buffer, 0, Length);


                    //notify whoever is listening of how far along the process we are
                    totalBytesReadSoFar += bytesRead;
                    job.OnProgress(this, new ProgressEventArgs(destinationFileName, new ProgressMeasurement(totalBytesReadSoFar / 1024, ProgressType.Kilobytes), s.Elapsed));
                }
                writeStream.Close();
            }
            
            response.Close();

            _filesRetrieved.Add(serverUri.ToString());
            s.Stop();
        }

        public virtual void LoadCompletedSoDispose(ExitCodeType exitCode,IDataLoadEventListener postLoadEventListener)
        {

            if (exitCode == ExitCodeType.Success && DeleteFilesOffFTPServerAfterSuccesfulDataLoad)
            {
                foreach (string file in _filesRetrieved)
                {
                    FtpWebRequest reqFTP;
                    reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(file));
                    reqFTP.Credentials = new NetworkCredential(_username, _password);
                    reqFTP.KeepAlive = false;
                    reqFTP.Method = WebRequestMethods.Ftp.DeleteFile;
                    reqFTP.UseBinary = true;
                    reqFTP.Proxy = null;
                    reqFTP.UsePassive = true;
                    reqFTP.EnableSsl = _useSSL;

                    FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();

                    if(response.StatusCode != FtpStatusCode.FileActionOK)
                        postLoadEventListener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "Attempt to delete file at URI " + file + " resulted in response with StatusCode = " + response.StatusCode));
                    else
                        postLoadEventListener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Deleted FTP file at URI " + file + " status code was " + response.StatusCode));

                    response.Close();
                }
            }
        }

        
        public void Check(ICheckNotifier notifier)
        {
            try
            {
                SetupFTP(_directory);
            }
            catch (Exception e)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("Failed to SetupFTP", CheckResult.Fail, e));
            }
        }
    }
}