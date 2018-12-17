﻿using Octokit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace YgoProPatcher
{

    public partial class YgoProPatcher : Form
    {
        public YgoProPatcher()
        {
            
            InitializeComponent();
            ServicePointManager.DefaultConnectionLimit = 85;
            string saveLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YgoProPatcher");
            string saveFile = Path.Combine(saveLocation, "paths.txt");
            if (Directory.Exists(saveLocation) && File.Exists(saveFile))
            {
                string[] paths = File.ReadAllLines(saveFile);
                YgoProLinksPath.Text = paths[0];
                YgoPro2Path.Text = paths[1];
            }
            _pool = new Semaphore(0, throttleValue);
            _pool.Release(throttleValue);
            toolTip1.SetToolTip(ReinstallCheckbox, "This will download the newest version of YGOPRO2 Client and install it.\nTHIS OPTION WILL OVERWRITE YOUR SETTINGS AND CUSTOM TEXTURES!");
            toolTip1.SetToolTip(OverwriteCheckbox, "This will redownload all the pics in your picture folder.");
            toolTip1.SetToolTip(gitHubDownloadCheckbox, "RECOMMENDED OPTION!\nThis will update your YGOPRO2 with newest cards, pictures and scripts.");
            toolTip1.SetToolTip(YgoPro2Path, "Please select Your YGOPRO2 Directory which contains all the YGOPRO2 files.");
            toolTip1.SetToolTip(YGOPRO2PathButton, "Please select Your YGOPRO2 Directory which contains all the YGOPRO2 files.");
            toolTip1.SetToolTip(YgoProLinksPath, "Please select Your YGOPRO Percy Directory which contains all the YGOPRO Percy files.");
            toolTip1.SetToolTip(YGOPRO1PathButton, "Plea" +
                "se select Your YGOPRO Percy Directory which contains all the YGOPRO Percy files.");
            toolTip1.SetToolTip(UpdateButton, "Start updating with selected options.");
            string version = Data.version;
            footerLabel.Text += version;
            CheckNewVersion(version);
        }
        int throttleValue = 80;
        int downloads = 0;
        bool threadRunning = false;
        static string token = Data.GetToken();
        private static Semaphore _pool;
        private void YgoProLinksButton_Click(object sender, EventArgs e)
        {
            FolderSelection("YGOPro Links");
        }

        private void YGOPRO2Button_Click(object sender, EventArgs e)
        {
            FolderSelection("YGOPRO2");
        }

        private void UpdateButton_Click(object sender, EventArgs e)
        {
            internetCheckbox.Enabled = false;
            UpdateButton.Enabled = false;
            ReinstallCheckbox.Enabled = false;
            gitHubDownloadCheckbox.Enabled = false;
            OverwriteCheckbox.Enabled = false;
            progressBar.Visible = true;
            exitButton.Visible = false;
            cancelButton.Visible = true;
            YgoPro2Path.Enabled = false;
            YGOPRO2PathButton.Enabled = false;
            threadRunning = true;
            backgroundWorker1.RunWorkerAsync();
        }

        private void Copy(string type)
        {
            string sourcePath = YgoProLinksPath.Text;
            string targetPath = YgoPro2Path.Text;
            string filePathYgoPro1 = "";
            string filePathYgoPro2 = "";
            string fileName;
            string destFile;

            if (type == "cdb")
            {
                filePathYgoPro1 = @"expansions\live2017links";
                filePathYgoPro2 = type;
            }
            if (type == "script")
            {
                filePathYgoPro1 = @"expansions\live2017links\" + type;
                filePathYgoPro2 = type;
            }
            if (type == "pic")
            {
                filePathYgoPro1 = type + "s";
                filePathYgoPro2 = @"picture\card";
            }
            if (type == "script2")
            {
                filePathYgoPro1 = type.Remove(type.Length - 1);

                filePathYgoPro2 = filePathYgoPro1;

            }
            try
            {
                string fileSource = System.IO.Path.Combine(sourcePath, filePathYgoPro1);
                string fileDestination = Path.Combine(targetPath, filePathYgoPro2);
                if (!(type == "script2"))
                {
                    Status.Invoke(new Action(() => { Status.Text = "Updating YGOPRO2 " + type + "s"; }));
                }
                else
                {
                    Status.Invoke(new Action(() => { Status.Text = "Updating old scripts"; }));
                }
                Status.Invoke(new Action(() => { progressBar.Value = 0; }));
                if (Directory.Exists(fileSource) && Directory.Exists(fileDestination))
                {
                    string partialName = "";
                    if (type == "cdb")
                    {
                        partialName = "prere*";
                    }
                    if (type == "pic")
                    {
                        partialName = "*.jpg";
                    }
                    if (type == "script" || type == "script2")
                    {
                        partialName = "*.lua";
                    }

                    string[] files = Directory.GetFiles(fileSource, partialName);

                    progressBar.Invoke(new Action(() => { progressBar.Maximum = files.Length; }));
                    foreach (string s in files)
                    {
                        if (threadRunning)
                        {
                            fileName = Path.GetFileName(s);

                            destFile = System.IO.Path.Combine(fileDestination, fileName);
                            if (type == "pic")
                            {
                                if (internetCheckbox.Checked)
                                {
                                    string website = Data.GetPicWebsite();
                                    if (!FileDownload(Path.ChangeExtension(fileName,".png"), fileDestination, website, OverwriteCheckbox.Enabled).Result)
                                    {
                                        destFile = Path.ChangeExtension(destFile, ".png");
                                        if (!File.Exists(destFile))
                                        {
                                            System.IO.File.Copy(s, destFile, false);
                                        }
                                    }
                                }
 
                            }
                            else
                            {
                                System.IO.File.Copy(s, destFile, true);
                            }
                                progressBar.Invoke(new Action(() => { progressBar.Increment(1); }));
                            
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (type == "cdb")
                    {
                        fileName = "lflist.conf";
                        destFile = System.IO.Path.Combine(System.IO.Path.Combine(targetPath, "config"), fileName);
                        System.IO.File.Copy(Path.Combine(fileSource, fileName), destFile, true);

                        fileName = "official.cdb";
                        destFile = System.IO.Path.Combine(fileDestination, fileName);
                        System.IO.File.Copy(Path.Combine(fileSource, fileName), destFile, true);

                        fileName = "cards.cdb";
                        fileDestination = Path.Combine(targetPath, type + @"\English");
                        destFile = System.IO.Path.Combine(fileDestination, fileName);
                        System.IO.File.Copy(Path.Combine(sourcePath, fileName), destFile, true);
                        fileName = "cards.cdb";
                        fileDestination = Path.Combine(targetPath, type);
                        destFile = System.IO.Path.Combine(fileDestination, fileName);
                        System.IO.File.Copy(Path.Combine(sourcePath, fileName), destFile, true);
                    }
                }
            }
            catch
            {
              
            }
        }

        private string FolderSelection(string versionOfYGO)
        {
            string folderString = "";
            FolderBrowserDialog fbd = new FolderBrowserDialog
            {
                ShowNewFolderButton = true,
                Description = "Select main folder of " + versionOfYGO
            };
            DialogResult result = fbd.ShowDialog();
            if (result == DialogResult.OK)
            {
                if (versionOfYGO == "YGOPRO2")
                {
                    YgoPro2Path.Text = fbd.SelectedPath;
                }
                else
                {
                    YgoProLinksPath.Text = fbd.SelectedPath;
                }


            }

            return folderString;
        }

        private async Task<bool> FileDownload(string fileName, string destinationFolder, string website, bool overwrite)
        {

            _pool.WaitOne();
            string webFile = website + fileName;
            string destFile;
            if (Path.GetExtension(fileName) == ".jpg")
            {
                destFile = Path.Combine(destinationFolder, Path.ChangeExtension(fileName, ".png"));
            }
            else
            {
                destFile = Path.Combine(destinationFolder, fileName);
            }


            try
            {

                if (!File.Exists(destFile) || overwrite)
                {

                    using (var client = new WebClient())
                    {
                        if (Path.GetExtension(fileName) == ".png")
                        {
                            client.Headers.Add(HttpRequestHeader.Authorization, string.Concat("token ", token));
                        }

                        await Task.Run(() => { client.DownloadFile(new Uri(webFile), destFile); });
                    }

                }

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                downloads = -_pool.Release();
                //debug.Invoke(new Action(() => { debug.Text = downloads.ToString(); }));
            }

        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            while (downloads > 1 - throttleValue && (gitHubDownloadCheckbox.Checked || internetCheckbox.Enabled))
            {
                Status.Invoke(new Action(() => { Status.Text = "Canceling the download, please wait!"; Status.Update(); }));
                
            }
           
            threadRunning = false;
            backgroundWorker1.CancelAsync();
            cancelButton.Visible = false;
            exitButton.Visible = true;
            internetCheckbox.Enabled = true;
            ReinstallCheckbox.Enabled = true;
            OverwriteCheckbox.Enabled = true;
            gitHubDownloadCheckbox.Enabled=true;
            YgoPro2Path.Enabled = true;
            YGOPRO2PathButton.Enabled = true;
            UpdateButton.Enabled = true;
            Status.Text = "Operation Canceled!";
            Status.Update();
        }

        private async void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            if (ReinstallCheckbox.Checked)
            {
               
                Status.Invoke(new Action(() => { Status.Text = "Reinstalling YGOPRO2, please be patient, this may take a while!";
                    cancelButton.Visible = false;
                    progressBar.Visible = false;
                }));

                YGOPRO2Client.Download(YgoPro2Path.Text);
            }
            cancelButton.Invoke(new Action(() =>
            {
                cancelButton.Visible = true;
                progressBar.Visible = true;
            }));
            
            DeleteOldCdbs();
            if (!gitHubDownloadCheckbox.Checked)
            {
                if (threadRunning) { Copy("cdb"); ; }
                if (threadRunning) { Copy("script"); }
                if (threadRunning) { Copy("script2"); }
                if (threadRunning) { Copy("pic"); ; }
            }
            else
            {
                if (threadRunning)
                {
                    await GitHubDownload(YgoPro2Path.Text);
                }
            }
            if (threadRunning)
            {

                Status.Invoke(new Action(() => { Status.Text = "Update Complete!"; ReinstallCheckbox.Enabled = true; cancelButton.Visible = false; exitButton.Visible = true; internetCheckbox.Enabled = true; gitHubDownloadCheckbox.Enabled = true; OverwriteCheckbox.Enabled = true; UpdateButton.Visible = false; FinishButton.Visible = true; FinishButton.Enabled = true; }));
                threadRunning = false;
            }
        }


        private async Task<List<string>> DownloadCDBSFromGithub(string destinationFolder)
        {
            
            List<string> listOfCDBs = GitAccess.GetAllFilesWithExtensionFromYGOPRO("/", ".cdb");
            string cdbFolder = Path.Combine(destinationFolder, "cdb");
            if(!await FileDownload("cards.cdb", cdbFolder, "https://github.com/szefo09/cdb/raw/master/", true))
            {
                await FileDownload("cards.cdb", cdbFolder, "https://github.com/shadowfox87/ygopro2/raw/master/cdb/", true);
            }
            progressBar.Invoke(new Action(() => progressBar.Maximum = listOfCDBs.Count));
            List<string> listOfDownloadedCDBS = new List<string>(){Path.Combine(cdbFolder,"cards.cdb" )};
            if (await FileDownload("prerelease-nfw.cdb", cdbFolder, "https://github.com/szefo09/cdb/raw/master/", true))
            {
                listOfDownloadedCDBS.Add(Path.Combine(cdbFolder, "prerelease-nfw.cdb"));
            }
            List<Task> downloadList = new List<Task>();
            foreach (string cdb in listOfCDBs)
            {
                
                FileDownload(cdb, cdbFolder, "https://github.com/Ygoproco/Live2017Links/raw/master/", true);
                listOfDownloadedCDBS.Add(Path.Combine(cdbFolder, cdb));
                progressBar.Invoke(new Action(() => progressBar.Increment(1)));

            }
            while (downloads > 1 - throttleValue)
            {
                Thread.Sleep(1);
            }
            return listOfDownloadedCDBS;
        }
        private void DownloadUsingCDB(List<string> listOfDownloadedCDBS, string destinationFolder)
        {
            if (threadRunning)
            {

                foreach (string cdb in listOfDownloadedCDBS)
                {
                    if (threadRunning)
                    {
                        DataClass db = new DataClass(cdb);
                        DataTable dt = db.SelectQuery("SELECT id FROM datas");
                        Status.Invoke(new Action(() => Status.Text = "Updating Pics and Scripts using " + Path.GetFileName(cdb)));
                        progressBar.Invoke(new Action(() => progressBar.Maximum = (dt.Rows.Count)));
                        progressBar.Invoke(new Action(() => progressBar.Value = 0));
                        string dlWebsitePics = Data.GetPicWebsite();
                        string dlWebsiteLua = Data.GetLuaWebsite();
                        string dlWebsitePicsCloseup = Data.GetPicsCloseUpWebsite();
                        string dFPics = Path.Combine(destinationFolder, @"picture\card");
                        string dFPicsCloseup = Path.Combine(destinationFolder, @"picture\closeup");
                        string dFLua = Path.Combine(destinationFolder, "script");
                        List<string> downloadList = new List<string>();

                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            if (threadRunning)
                            {
                                downloadList.Add(dt.Rows[i][0].ToString());
                            }
                            else
                            {
                                break;
                            }

                        }
                        foreach (string Value in downloadList)
                        {

                            if (threadRunning)
                            {
                                FileDownload(Value.ToString() + ".png", dFPics, dlWebsitePics, OverwriteCheckbox.Checked);
                                FileDownload("c" + Value.ToString() + ".lua", dFLua, dlWebsiteLua, true);
                                FileDownload(Value.ToString() + ".png", dFPicsCloseup, dlWebsitePicsCloseup, OverwriteCheckbox.Checked);
                                progressBar.Invoke(new Action(() => progressBar.Increment(1)));

                            }
                        }
                        while (downloads > 1 - throttleValue)
                        {
                            Thread.Sleep(1);
                        }

                    }

                }
                while (downloads > 1-throttleValue)
                {
                    Thread.Sleep(1);
                }
                if (threadRunning) { 
                GitHubClient client = new GitHubClient(new ProductHeaderValue("pics"))
                {
                    Credentials = new Credentials(token)
                };
                string path = "picture/field";
                var fields = client.Repository.Content.GetAllContents("shadowfox87", "YGOSeries10CardPics", path).Result;
                Status.Invoke(new Action(() => { Status.Text = "Downloading Fieldspell Pics"; }));
                progressBar.Invoke(new Action(() => { progressBar.Maximum = fields.Count; }));
                foreach (var field in fields)
                {
                    if (threadRunning)
                    {
                        FileDownload(field.Name, Path.Combine(YgoPro2Path.Text, path), field.DownloadUrl, OverwriteCheckbox.Enabled);
                        progressBar.Invoke(new Action(() => { progressBar.Increment(1); }));
                    }
                }
                while (downloads > 1-throttleValue)
                {
                    Thread.Sleep(1);
                }
            }
        }
        }

        private async Task GitHubDownload(string destinationFolder)
        {
            Status.Invoke(new Action(() => { Status.Text = "Updating CDBS from Live2017Links"; }));
            List<string> CDBS = new List<string>();

            CDBS = await DownloadCDBSFromGithub(destinationFolder);
            await FileDownload("lflist.conf", Path.Combine(YgoPro2Path.Text, "config"), "https://raw.githubusercontent.com/Ygoproco/Live2017Links/master/", true);
            await FileDownload("strings.conf", Path.Combine(YgoPro2Path.Text, "config"), Data.GetStringsWebsite(), true);
            progressBar.Invoke(new Action(() => { progressBar.Value = progressBar.Maximum; }));

            DownloadUsingCDB(CDBS, destinationFolder);

        }

        private void GitHubDownloadCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            YgoProLinksPath.Enabled = !YgoProLinksPath.Enabled;
            YGOPRO1PathButton.Enabled = !YGOPRO1PathButton.Enabled;
            YgoProLinksPath.Visible = !YgoProLinksPath.Visible;
            YGOPRO1PathButton.Visible = !YGOPRO1PathButton.Visible;
            YGOPRO1Label.Visible = !YGOPRO1Label.Visible;
            internetCheckbox.Enabled = !internetCheckbox.Enabled;
            if (gitHubDownloadCheckbox.Checked&&!internetCheckbox.Checked)
            {
                
                internetCheckbox.Checked = !internetCheckbox.Checked;
            }


        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        private void YgoProPatcher_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (threadRunning) {
                threadRunning = false;
                while (downloads > 1 - throttleValue &&(gitHubDownloadCheckbox.Checked || internetCheckbox.Enabled))
                {
                    Status.Invoke(new Action(() => { Status.Text = "Canceling the download, please wait!"; Status.Update(); }));

                }
            }
            string saveLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YgoProPatcher");
            string[] locationPaths = { YgoProLinksPath.Text, YgoPro2Path.Text };
            if (!Directory.Exists(saveLocation))
            {
                Directory.CreateDirectory(saveLocation);
            }
            File.WriteAllLines(Path.Combine(saveLocation, "paths.txt"), locationPaths);
        }

        private void DeleteOldCdbs()
        {
            try
            {
                string cdbFolder = Path.Combine(YgoPro2Path.Text, "cdb");
                FileInfo[] cdbFiles = new DirectoryInfo(cdbFolder).GetFiles();
                foreach (FileInfo cdb in cdbFiles)
                {
                    if (cdb.Name.Contains("prerelease") || cdb.Name.Contains("fix"))
                    {
                        cdb.Delete();
                    }
                }
            }
            catch(Exception e)
            {
                MessageBox.Show("Access to YGOPRO2 denied. Check if the path is correct or\ntry launching the Patcher with Admin Privileges.\n\nError Code:\n"+e.ToString());
                threadRunning = false;
                cancelButton.Visible = false;
                
            }
        }
        private void CheckNewVersion(string version)
        {
            try
            {
                Release release = GitAccess.GetNewestYgoProPatcherRelease();
                if (release.TagName!=null && release.TagName != version && MessageBox.Show("New version of YgoProPatcher detected!\nDo You want to download it?", "New Version detected!", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    string dir;
                    FolderBrowserDialog fbd = new FolderBrowserDialog
                    {
                        ShowNewFolderButton = true,
                        SelectedPath = System.Windows.Forms.Application.StartupPath,
                        Description = "Select where you want to download YgoProPatcher ZIP File:"
                    };
                    DialogResult result = fbd.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        dir = fbd.SelectedPath;
                    }
                    else
                    {
                        return;
                    }
                    string fileName = Path.Combine(dir, "YgoProPatcher" + release.TagName + ".zip");
                    using (WebClient client = new WebClient())
                    {

                        client.DownloadFile(new System.Uri(release.Assets[0].BrowserDownloadUrl), fileName);
                    }
                    if (new FileInfo(fileName).Exists)
                    {
                        MessageBox.Show("New YgoProPatcher" + release.TagName + ".zip was succesfully\ndownloaded to the target location.\nPlease extract the newest release and use it!\n\nThis app will now close.", "Download Completed!");
                        try
                        {
                            System.Diagnostics.Process.Start(fileName);
                        }
                        catch
                        {
                        }
                        finally
                        {
                            Environment.Exit(0);
                        }
                    }
                   
                }
            }
            catch
            { 
                MessageBox.Show("Couldn't check for new version of YgoProPatcher.\nMake sure You are connected to the internet or no program blocks the Patcher!");
            }
        }

        private void FinishButton_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo ygopro2= new System.Diagnostics.ProcessStartInfo(Path.Combine(YgoPro2Path.Text, "YGOPro2.exe"));
                ygopro2.WorkingDirectory = YgoPro2Path.Text;
                System.Diagnostics.Process.Start(ygopro2);
            }
            catch
            {

            }
            finally
            {
                Environment.Exit(0);
            }

        }
    }
   
}