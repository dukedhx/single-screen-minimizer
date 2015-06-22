using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Security;



namespace WindowsFormsApplication4
{
    [SuppressUnmanagedCodeSecurity]
    public partial class Form1 : Form
    {
        LinkedList<IntPtr> wins;
        Boolean mts, mined,bound;
        Keys key;
        LinkedList<ComboBox> cmbs;
        static IntPtr _hookID = IntPtr.Zero;
      const int WM_KEYDOWN = 0x0100,SW_SHOWMINNOACTIVE = 7;
      [DllImport("user32.dll")]
      static extern int GetWindowLong(IntPtr hWnd, int nIndex);

      [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]

      internal static extern int GetWindowText(IntPtr hWnd, [Out] StringBuilder lpString, int nMaxCount);
      [DllImport("user32.dll")]
      static extern short GetAsyncKeyState(int vKey);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumThreadDelegate enumProc, IntPtr lParam);
                   
        delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);
       

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);

       
        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int Left, Top, Right, Bottom;       
        }

        [Serializable]
        private class KeyObj
        {
            public String text { get; set; }
            public Keys key { get; set; }
            internal KeyObj() { text = ""; key = Keys.None; }
            public override string ToString()
            {
                return text;
            }
        }
       
        delegate IntPtr LowLevelKeyboardProc(
       int nCode, IntPtr wParam, IntPtr lParam);

        Class1 sb;
        Keys[] comb;
        void loadScreens()
        {
            comboBox1.DataSource = (from screen in Screen.AllScreens select screen.DeviceName.Split('\\').Last()).ToArray();
        }

        public Form1()
        {
            InitializeComponent(); comboBox1.DropDown += (s, e) => { loadScreens(); }; mts = mined = bound = false;
            wins=new LinkedList<IntPtr>();
            sb = new Class1(minWins);
            loadScreens();
            cmbs = new LinkedList<ComboBox>();
            KeyObj nv=new KeyObj();
            LinkedList<KeyObj> ansl = new LinkedList<KeyObj>();
            ansl.AddLast(nv); 
            foreach (Keys key in Enum.GetValues(typeof(Keys)))
            {
                String value = key.ToString();
                if (Regex.IsMatch(value, @"(^[A-Z]$)|(^[DF]([0-9]$)|(1[0-2]$))"))
                    ansl.AddLast(new KeyObj() { key = key, text = value.Length > 1 ? value.Replace("D", "") : value });                
            }
            KeyObj[] funcs = new KeyObj[] { nv, new KeyObj() { key = Keys.LMenu, text = "Left Alt" }, new KeyObj() { key = Keys.LControlKey, text = "Left CTRL" }, new KeyObj() { key = Keys.LShiftKey, text = "Left Shift" },
            new KeyObj() { key = Keys.RMenu, text = "Right Alt" }, new KeyObj() { key = Keys.RControlKey, text = "Right CTRL" }, new KeyObj() { key = Keys.RShiftKey, text = "Right Shift" }};
           
            prepareCombo(comboBox2, funcs); 
            prepareCombo(comboBox3, funcs.Clone() as KeyObj[]); 
            prepareCombo(comboBox4, ansl.ToArray());
            try
            {                
                if (File.Exists(Properties.Resources.combpath))
                    using (Stream stream = File.Open(Properties.Resources.combpath, FileMode.Open))
                    {
                       IEnumerator<int> kobjs = (new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter().Deserialize(stream) as LinkedList<int>).GetEnumerator();
                        foreach(ComboBox cmb in cmbs) if(kobjs.MoveNext())cmb.SelectedIndex=kobjs.Current;
                        bind(false);
                        if (kobjs.MoveNext()) comboBox1.SelectedIndex = kobjs.Current>=comboBox1.Items.Count||kobjs.Current<0?0:kobjs.Current;
                        if (kobjs.MoveNext()) checkBox1.Checked = kobjs.Current>0;
                        if (kobjs.MoveNext()) checkBox2.Checked = kobjs.Current > 0;
                    }
            }
            catch (Exception ex)
            {
                Debugger.Log(0,"",ex.Message);
            }
            if (!bound) label1.Text = Properties.Resources.defaultcap;
           // checkBox2.CheckedChanged += (s, e) => { if (!checkBox2.Checked)wins.Clear(); };
        }

        void showballontip(String title, String text)
        {
            if (checkBox1.Checked) { notifyIcon1.BalloonTipText = text; notifyIcon1.BalloonTipTitle = title; notifyIcon1.ShowBalloonTip(30000); }
        }

        void prepareCombo(ComboBox cmb, KeyObj[] kobjs)
        {    
           cmb.DisplayMember = "text";
           cmb.ValueMember = "key";
           cmb.DataSource = kobjs;           
            cmbs.AddLast(cmb);
            cmb.SelectionChangeCommitted += (o, e) =>
            {
                checkbindable();
               
            };
        }

        void checkbindable()
        {
            Boolean enabled = false;
            if (!bound)
                enabled = !cmbs.Any(c => c.SelectedIndex <1);
            else
            {
                IEnumerator<ComboBox> cenu = getEnu(cmbs);
                while (cenu != null)
                {
                    Keys k = (Keys)cenu.Current.SelectedValue;
                    if (!cenu.MoveNext()) { if (!key.Equals(k)) enabled = true; break; }
                    if (k.Equals(Keys.None)) { enabled = false; break; }
                    if (!enabled && comb != null && !comb.Contains(k)) enabled = true;
                }
            }
            button1.Enabled = enabled;
            if(!bound)toolStripMenuItem2.Enabled = enabled;
        }

        IEnumerator<T> getEnu<T>(IEnumerable<T> list)
        {
            IEnumerator<T> cenu = list.GetEnumerator();
            return cenu.MoveNext()?cenu:null;
        }

        void bind(Boolean save)
        {
            comb = (from cmb in cmbs where cmb != cmbs.Last() select (Keys)cmb.SelectedValue).ToArray();
            key = (Keys)cmbs.Last().SelectedValue;
            hook();        
            label1.Text = "Current: " + String.Join("+", (from cmb in cmbs select cmb.Text).ToArray());
            if (bound) showballontip(Properties.Resources.shortcutchanged, label1.Text);
            else
            {
                bound = true;
                showballontip(Properties.Resources.nstitle, String.Format(Properties.Resources.nstext, getComb));
            }
            if (save)
                Save();
            toolStripMenuItem2.Enabled = true;
            toolStripMenuItem2.Text = "Unbind";
            button1.Enabled = false;
        }

        void Save()
        {
            try
            {
                using (Stream stream = File.Open(Properties.Resources.combpath, FileMode.Create))
                {
                    LinkedList<int> kobjs = new LinkedList<int>();
                    foreach (ComboBox cmb in cmbs) kobjs.AddLast(cmb.SelectedIndex);
                    kobjs.AddLast(comboBox1.SelectedIndex);
                    kobjs.AddLast(Convert.ToInt32(checkBox1.Checked)); kobjs.AddLast(Convert.ToInt32(checkBox2.Checked));
                    new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter().Serialize(stream, kobjs);
                    showballontip("Saved", "Settings Saved");
                }
            }
            catch (Exception ex)
            {
                showballontip("Error",ex.Message);
            }
        }
       
       void button1_Click(object sender, EventArgs e)
        {             
            bind(true);          
    }

       void frmMain_Resize(object sender, EventArgs e)
       {
           if (FormWindowState.Minimized == this.WindowState)
               minself();

           else if (FormWindowState.Normal == this.WindowState)
               openwin();
           
       }

       void minself()
       {
           if (!mts)
           {
               showballontip(Properties.Resources.ntrtitle, Properties.Resources.ntrtext); mts = true;
           }
           this.Hide();
           mined = true;
           toolStripMenuItem1.Text = "Show";
       }

         void minWin(IntPtr handle,Screen screen)
        {
            RECT rct;
            if (GetWindowRect(handle, out rct))
            {
                Rectangle myRect = new Rectangle();
                myRect.X = rct.Left;
                myRect.Y = rct.Top;
                myRect.Width = rct.Right - rct.Left + 1;
                myRect.Height = rct.Bottom - rct.Top + 1;
              
                if (screen.WorkingArea.IntersectsWith(myRect))
                {
                  //  Debugger.Log(0, "SB", "SB233:");
                    long value =GetWindowLong(handle, -20);
                    if ((value & 0x00040000L) != 0 || ((value & 0x00000080L) == 0 && GetWindowLong(handle, -8) == 0))
                    {
                        StringBuilder stringBuilder = new StringBuilder(256);
                        GetWindowText(handle, stringBuilder, stringBuilder.Capacity);
                       if(!String.IsNullOrEmpty(stringBuilder.ToString()))
                        {
                    //    Debugger.Log(0,"SB",stringBuilder.ToString()+"\n");
                        ShowWindow(handle, 7);
                        //    if(checkBox2.Checked)
                        wins.AddLast(handle);
                       }
                        }
                 //  
                }
            }            
        }
         
         void minWins()
        {
            
            if (wins.Count > 0)
            {
                if(checkBox2.Checked)
                foreach (IntPtr handle in wins)
                    ShowWindow(handle, 4);
                wins.Clear();
                if (checkBox2.Checked) return;
            }
           
              //  Debugger.Log(0, "SB", "\n<<<minwin start>>>\n");
                int idx = comboBox1.SelectedIndex;
                Screen[] screens = Screen.AllScreens;
                if (screens.Length <= idx) { showballontip(Properties.Resources.dcctitle, Properties.Resources.dcctext); loadScreens(); return; }
                Screen screen = Screen.AllScreens[idx];
                if (EnumWindows((h, l) =>
                {
                    if (IsWindowVisible(h)) minWin(h, screen);
                    return true;
                }, IntPtr.Zero))
                    showballontip(Properties.Resources.keypressed, comboBox1.Text + " " + Properties.Resources.minimized);
              //  Debugger.Log(0, "SB", "<<<minwin end>>>\n");
            
        }       

     IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)                           
               if(((Keys)Marshal.ReadInt32(lParam)).Equals(key))
                   if (comb.All(k =>  0 != (GetAsyncKeyState((int)k) & 0x8000))) minWins();    
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }     
               
        void unhook()
        {
            if (sb != null) sb.unregister();
         
        }

        void unbind()
        {
            unhook(); bound = false;
            showballontip(Properties.Resources.cutitle, String.Format(Properties.Resources.cutext, getComb));
                toolStripMenuItem2.Text = "Bind";
                checkbindable();

                label1.Text = Properties.Resources.defaultcap;
        }

        String getComb
        {
            get
            {
                return label1.Text.Split(':').Last();
            }
        }

        void hook()
        {
            ModifierKey mk1=getKey(comb[0]), mk2=getKey(comb[1]);

            sb.Register(mk1==mk2?mk1:mk1 | mk2, key);
        }

        ModifierKey getKey(Keys key)
        {
            switch(key){
                case Keys.LMenu: return ModifierKey.Alt;
                case Keys.LControlKey: return ModifierKey.Control;
                case Keys.LWin: return ModifierKey.Win;
                case Keys.LShiftKey: return ModifierKey.Shift;
            }
            return ModifierKey.Alt|ModifierKey.Control;

        }

        void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
           String name= e.ClickedItem.Name;
           if (name.Equals(toolStripMenuItem1.Name)) decideWinstate();
           else if (name.Equals(toolStripMenuItem2.Name)) if (bound) unbind(); else bind(true);
           else if (name.Equals(toolStripMenuItem3.Name)) this.Close();
           else if (name.Equals(toolStripMenuItem4.Name)) Save();

        }
        
        void openwin()
        {
            toolStripMenuItem1.Text = "Hide";
           this.Show();
           this.WindowState = FormWindowState.Normal; 
           this.BringToFront();
          
            mined = false;
        }

        void decideWinstate()
        {
            if (mined) openwin(); else minself(); 
        }

       void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            decideWinstate();
        }
}

        }
