using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Архиватор_Pack
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        string fullPath; 
        void DriveTreeInit() 
        {
            
            string[] drivesArray = Directory.GetLogicalDrives();

            treeView1.BeginUpdate(); 
            treeView1.Nodes.Clear();
            
            foreach (string drive in drivesArray)
            {
                TreeNode drivenode = new TreeNode(drive, 0, 1);
                
                treeView1.Nodes.Add(drivenode);
                
                GetDirs(drivenode);
            }
            treeView1.EndUpdate(); 
        }        
        
        void GetDirs(TreeNode node)
        {
            node.Nodes.Clear();
            
            fullPath = node.FullPath;
            
            DirectoryInfo di = new DirectoryInfo(fullPath);
            
            DirectoryInfo[] diArray;
            try
            {
                diArray = di.GetDirectories();
            }
            
            catch { return; }
            
            foreach (DirectoryInfo dirinf in diArray)
            {
                TreeNode dir = new TreeNode(dirinf.Name, 0, 1);
                node.Nodes.Add(dir);               
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            DriveTreeInit();            
        }

        private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            treeView1.BeginUpdate();
            foreach(TreeNode node in e.Node.Nodes)            
                
                GetDirs(node);            
            treeView1.EndUpdate();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode sn = e.Node;
            
            fullPath = sn.FullPath;
            
            DirectoryInfo di = new DirectoryInfo(fullPath);
            DirectoryInfo[] diArray;
            FileInfo[] fArray;
            try
            {
                diArray = di.GetDirectories();
                fArray = di.GetFiles();
            }
            catch { return; }

            listView1.Items.Clear();
            
            foreach (DirectoryInfo dir in diArray)
            {
                ListViewItem l = new ListViewItem(dir.Name);
                l.SubItems.Add(""); 
                
                string t = dir.LastWriteTime.ToString();

                if (t.Length == 18)
                    t = t.Insert(11, "0");
                
                l.SubItems.Add(t);
                listView1.Items.Add(l);
            }

            foreach (FileInfo fil in fArray)
            {
                long a; 
                ListViewItem l = new ListViewItem(fil.Name);
                a = fil.Length / 1024;
                string s = fil.Length.ToString();

                for (int i = s.Length - 3; i > 0; i -= 3)
                    s = s.Insert(i, " ");

                l.SubItems.Add(s);

                string t = fil.LastWriteTime.ToString();

                if (t.Length == 18)
                    t = t.Insert(11, "0");
                    
                l.SubItems.Add(t);
                listView1.Items.Add(l);
            }

        }

        void pack(string fileSource, string filePack, ref int Err)
        {

            if (!File.Exists(fileSource))
            {
                Err = -1;
                return;
            }
            BinaryReader br = new BinaryReader(File.Open(fileSource, FileMode.Open));
            BinaryWriter bw = null;
            try
            {
                bw = new BinaryWriter(File.Open(filePack, FileMode.Create));
            }
            catch
            {
                br.Close();
                Err = -2;
                return;
            }
            bw.Write(Path.GetFileName(fileSource));
            List<byte> bt = new List<byte>();
            long len = br.BaseStream.Length;
            while (br.BaseStream.Position < len)
            {
                byte B = br.ReadByte(); // тут хранятся позиция байта ну грубо говоря если 00 то записывает без повторов
                if (bt.Count == 0)
                    bt.Add(B);
                else
                {
                    if (B != bt[bt.Count - 1])
                    {
                        bt.Add(B);
                        if (bt.Count == 255)
                        {
                            bw.Write((byte)0);
                            bw.Write((byte)255);//длина цепочки
                            //вывести в файл содержимое буфера
                            bw.Write(bt.ToArray(), 0, 255);
                            bt.Clear(); //очистить буфер
                        }
                    }
                    else // если прочитанный байт совпадает с последним байтом в буфере
                    {
                        //если в буфере есть неповторяющиеся байты
                        if (bt.Count > 1)
                        {
                            bw.Write((byte)0);
                            bw.Write((byte)(bt.Count - 1));
                            bw.Write(bt.ToArray(), 0, bt.Count - 1);
                            bt.RemoveRange(0, bt.Count - 1);
                        }
                        bt.Add(B);

                        if (br.BaseStream.Position < len)
                        {
                            while ((B = br.ReadByte()) == bt[0])
                            {
                                bt.Add(B);
                                if (bt.Count == 255)
                                {
                                    bw.Write((byte)255);
                                    bw.Write(bt[0]);
                                    bt.Clear();
                                    break;
                                }
                                if (br.BaseStream.Position == len)
                                    break;
                            }
                        }

                        if (bt.Count > 0)
                        {
                            bw.Write((byte)bt.Count);
                            bw.Write(bt[0]);
                            bt.Clear();
                            if (br.BaseStream.Position < len)
                                bt.Add(B);
                        }

                    }
                }
            }

            if (bt.Count > 0)
            {
                bw.Write((byte)0);
                bw.Write((byte)bt.Count);
                bw.Write(bt.ToArray(), 0, bt.Count);
            }

            br.Close();
            bw.Close();

        }

        void unpack(string filePack, string folder)
        {
            BinaryReader br = new BinaryReader(File.Open(filePack, FileMode.Open));
            string fileSourse = br.ReadString();
            fileSourse = folder + "\\" + fileSourse;
            BinaryWriter bw = new BinaryWriter(File.Open(fileSourse, FileMode.Create));

            long len = br.BaseStream.Length;
            while (br.BaseStream.Position < len)
            {
                byte B = br.ReadByte();
                if (B == 0)
                {
                    byte lnr = br.ReadByte();
                    for (int i = 1; i <= lnr; i++)
                    {
                        B = br.ReadByte();
                        bw.Write(B);
                    }
                }
                else
                {
                    byte value = br.ReadByte();
                    for (int i = 1; i <= B; i++)
                        bw.Write(value);
                }

            }


            br.Close(); bw.Close();
        }


        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            this.Text = fullPath + "\\" + listView1.SelectedItems[0].Text;
        }

        private void fPack_Click(object sender, EventArgs e)
        {
            if(listView1.SelectedItems.Count == 0)
            {
                MessageBox.Show("Нет выделенного элемента.");
                return;
            }
            string fsourse, fpack;
            fsourse = fullPath + "\\" + listView1.SelectedItems[0].Text;
            fpack = Path.GetDirectoryName(fsourse) + @"\" +
                    Path.GetFileNameWithoutExtension(fsourse) + ".pack";
            if(!File.Exists(fsourse))
            {
                MessageBox.Show("Выбранный файл не существует.");
                return;
            }
            int err = 0;
            bool alreadyexists = File.Exists(fpack);
            pack(fsourse,fpack,ref err);
            if (err == 0)
                MessageBox.Show("Упаковка в архив закончена", "Информация для пользователя", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else if (err == -1)
                MessageBox.Show("Файла для архирования нет", "Информация для пользователя", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else if (err == -2)
                MessageBox.Show("Невозможно открыть файл", "Информация для пользователя", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("Возникла неизвестная ошибка", "Информация для пользователя", MessageBoxButtons.OK, MessageBoxIcon.Information);          
            if (!alreadyexists && File.Exists(fpack))
            {
                ListViewItem lvItem = new ListViewItem(Path.GetFileName(fpack));
                FileInfo fi = new FileInfo(fpack);
                string dl = fi.Length.ToString();
                for (int i = dl.Length - 3; i >= 0; i -= 3)
                    dl = dl.Insert(i," ");
                lvItem.SubItems.Add(dl);

                string t = fi.LastWriteTime.ToString();
                if (t.Length == 18)
                    t = t.Insert(11,"0");
                lvItem.SubItems.Add(t);
                int nul = listView1.SelectedIndices[0];
                listView1.Items.Insert(nul+1,lvItem);
            }
        }

        private void fUnPack_Click(object sender, EventArgs e)
        {
            //если нет выделенных файлов вывести сообщение
            if (listView1.SelectedItems.Count == 0)
            {
                MessageBox.Show("Нет выделенного элемента.");
                return;
            }
            string fpack = fullPath + "\\" + listView1.SelectedItems[0].Text;
            //имя исходного файла из архива
            string namefromarh;
            //получить имя исходного файла
            BinaryReader br = new BinaryReader(File.Open(fpack, FileMode.Open));
            namefromarh = br.ReadString();
            namefromarh = fullPath + "\\" + namefromarh;
            br.Close();
            bool alreadyexists = File.Exists(namefromarh);            
            //открыть архив для чтения и прочитать строку
            try
            {                
                unpack(fpack, fullPath);
            }
            catch
            {
                MessageBox.Show("Файл поврежден или не является архивом.");                
            }
            if (!alreadyexists && File.Exists(namefromarh))
            {
                ListViewItem lvItem = new ListViewItem(Path.GetFileName(namefromarh));
                FileInfo fi = new FileInfo(namefromarh);
                string dl = fi.Length.ToString();
                for (int i = dl.Length - 3; i >= 0; i -= 3)
                    dl = dl.Insert(i, " ");
                lvItem.SubItems.Add(dl);

                string t = fi.LastWriteTime.ToString();
                if (t.Length == 18)
                    t = t.Insert(11, "0");
                lvItem.SubItems.Add(t);
                int nul = listView1.SelectedIndices[0];
                listView1.Items.Insert(nul + 1, lvItem);

                
            }
            FileInfo archfi = new FileInfo(namefromarh);
            FileInfo sourcefi = new FileInfo(fpack);
            double sizearch = archfi.Length;
            double sizesource = sourcefi.Length;

            MessageBox.Show("Распаковка завершена \r\n \r\n Размер исходного файла: " + sizesource +
                "\r\n Размер упакованного файла: " + sizearch + "\r\n Это " + Math.Round(sizesource / sizearch * 100) + " % от исходного файла");
        }
    }
}
