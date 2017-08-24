using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Compression;
using System.Linq;
using CatalogueLibrary.Data;
using CatalogueLibrary.Repositories;
using ReusableLibraryCode.Checks;

namespace RDMPStartup.PluginManagement
{
    public class PluginProcessor
    {
        private readonly ICheckNotifier _notifier;
        private readonly CatalogueRepository _repository;

        public PluginProcessor(ICheckNotifier notifier,CatalogueRepository repository)
        {
            _notifier = notifier;
            _repository = repository;
        }

        public bool ProcessFileReturningTrueIfIsUpgrade(FileInfo toCommit)
        {
            bool toReturn = false;

            if (toCommit.Extension != ".zip")
                throw new NotSupportedException("Files must be zip files");
            
            var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

            ZipFile.ExtractToDirectory(toCommit.FullName, workingDirectory);

            //delete old versions of the file
            var oldVersions = _repository.GetAllObjects<Plugin>().Where(p => p.Name.Equals(toCommit.Name));
            
            foreach (Plugin p in oldVersions)
            {
                p.DeleteInDatabase();
                toReturn = true;
            }

            var plugin = new Plugin(_repository, toCommit);

            try
            {
                foreach (var file in Directory.GetFiles(workingDirectory, "*.dll"))
                    ProcessFile(plugin, new FileInfo(file));

                foreach (var srcZipFile in Directory.GetFiles(workingDirectory,"src.zip"))
                    ProcessFile(plugin, new FileInfo(srcZipFile));
                

            }
            catch (Exception e)
            {
                _notifier.OnCheckPerformed(new CheckEventArgs("Failed processing plugin " + toCommit.Name,
                    CheckResult.Fail, e));
                throw;
            }
            finally
            {
                //make sure we always delete the working directory
                Directory.Delete(workingDirectory, true);
            }

            return toReturn;
        }

        private void ProcessFile(Plugin plugin, FileInfo toCommit)
        {

            if (LoadModuleAssembly.IsDllProhibited(toCommit))
                return;

            try
            {
                new LoadModuleAssembly(_repository,toCommit,plugin);
                _notifier.OnCheckPerformed(new CheckEventArgs("File " + toCommit.Name + " uploaded as a new LoadModuleAssembly under plugin " + plugin.Name, CheckResult.Success));
            }
            catch (Exception e)
            {
                _notifier.OnCheckPerformed(new CheckEventArgs("Failed to construct new load module assembly",CheckResult.Fail, e));
            }
        }
        /*
        private bool CheckAssemblyAndManifestAgreement(string workingDirectory, Version pluginCatalogueVersionFromManifest)
        {
            // Single: there must be one and only one *Plugin.dll file
            var assemblyFile = Directory.GetFiles(workingDirectory, "*Plugin.dll", SearchOption.TopDirectoryOnly).Single();

            // Create a new AppDomain so we can do our assembly testing in a clean environment and then unload it afterwards (otherwise if the version check fails we can't cleanup the directory because the assembly has been loaded and has a current handle in the current AppDomain)
            var testDomain = AppDomain.CreateDomain("TestDomain", null, AppDomain.CurrentDomain.SetupInformation);

            try
            {
                // Unfortunately, loading an assembly into a separate domain through domain.Load also loads it into the current domain!
                // So to avoid polluting CurrentDomain with unwanted/potentially dangerous assemblies, we need a bit of marshalling magic (and a separate class which can be marshalled).
                var assemblyChecker =
                    (PluginManifestAgreementChecker)
                        testDomain.CreateInstanceAndUnwrap(typeof (PluginManifestAgreementChecker).Assembly.FullName,
                            typeof (PluginManifestAgreementChecker).FullName);

                var versionsAgree = assemblyChecker.CheckPlugin(assemblyFile, pluginCatalogueVersionFromManifest, _notifier);
                if (!versionsAgree)
                    _notifier.OnCheckPerformed(new CheckEventArgs(assemblyChecker.LastMessage, CheckResult.Fail));

                return versionsAgree;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            finally
            {
                AppDomain.Unload(testDomain);
            }
        }*/
    }
}