using System;
using WixSharp;
using System.Diagnostics;
using Microsoft.Deployment.WindowsInstaller;

namespace TestApp.Installer
{
    using System.IO;
    using File = WixSharp.File;

    internal class Program
    {
        private static void Main()
        {

#if DEBUG
#error "Do not distribute Debug builds"
#endif
            const string guid = "5976a455-fa1f-44d2-91fa-53cc2beca27f";     // Change for every product!
            const string outdir = "build";                                  // directory is relative to solution root
            const string product = "Pyramid RS-232 Test App";
            const string company = "Pyramid Technologies Inc";
            const string binname = "PyramidNETRS232_TestApp.exe";
            const string config = "PyramidNETRS232_TestApp.exe.config";
            const string rootdir = @"..\Apex7000_BillValidator_Test\bin\Release";

            var baseDir = Environment.CurrentDirectory;

            // Installer logos and license
            var bannerImage = Path.Combine(baseDir, "app.dialog_banner.bmp");
            var backgroundImage = Path.Combine(baseDir, "app.dialog_bmp.bmp");
            var licenceFile = Path.Combine(baseDir, "app.license.rtf");

            var project = new ManagedProject(product,
                new Dir(@"%ProgramFiles%\" + company + "\\" + product,
                    new Files("*.xml"),
                    new Files("*.dll"),
                    new File(binname,
                        new FileShortcut(product, @"%Desktop%") { IconFile = @"icon.ico" },
                        new FileShortcut(product, @"%ProgramMenu%\" + company + "\\" + product) { IconFile = @"icon.ico" }),
                    new File(config)))
            {
                SourceBaseDir = rootdir,
                ControlPanelInfo =
                {
                    Comments = "RS-232 Test Application for Pyramid Bill Acceptors",
                    Readme = "https://pyramidacceptors.com/",
                    HelpLink = "https://pyramidacceptors.com/",
                    HelpTelephone = "480-641-9763",
                    UrlInfoAbout = "https://pyramidacceptors.com/",
                    UrlUpdateInfo = "https://pyramidacceptors.com/",
                    ProductIcon = "icon.ico",
                    Contact = "support@pyramidacceptors.com",
                    Manufacturer = company
                },
                GUID = new Guid(guid)
            };


            // Extract build revision from the file that is compiled into this MSI
            var version = FileVersionInfo.GetVersionInfo(System.IO.Path.Combine(rootdir, binname));
            project.Version = new Version(version.FileVersion);

            project.MajorUpgradeStrategy = MajorUpgradeStrategy.Default;
            project.MajorUpgradeStrategy.RemoveExistingProductAfter = Step.InstallInitialize;

            project.BannerImage = bannerImage;
            project.BackgroundImage = backgroundImage;
            project.ValidateBackgroundImage = false;
            project.LicenceFile = licenceFile;

            project.OutDir = outdir;
            project.BuildMsi(string.Format(@"..\..\..\build\{0}-installer.{1}.msi", product, project.Version));
        }
    }
}
