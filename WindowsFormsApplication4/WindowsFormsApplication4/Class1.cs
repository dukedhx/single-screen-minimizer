using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsFormsApplication4
{
    [Flags]
    public enum ModifierKey : uint
    {
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8
    }


 public class Class1 : IDisposable
{

[DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);



    public delegate void KeyPressed();
    private class Window : NativeWindow, IDisposable
    {
        private static int WM_HOTKEY = 0x0312;
        KeyPressed kp;
        public Window(KeyPressed akp)
        {
            kp = akp;
            this.CreateHandle(new CreateParams());
        }
      
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_HOTKEY)
                kp();
        }

       

        #region IDisposable Members

        public void Dispose()
        {
            this.DestroyHandle();
        }

        #endregion
    }

    private Window _window ;
    private int _currentId;

    public Class1(KeyPressed akp)
    {        
        _window=new Window(akp);
    }

    public void Register(ModifierKey modifier, Keys key)
    {
      
        _currentId = _currentId + 1;

        if (!RegisterHotKey(_window.Handle, _currentId, (uint)modifier, (uint)key))
            throw new InvalidOperationException("Couldn’t register the hot key.");
    } 
  

    #region IDisposable Members


    public void unregister()
    {
        for (int i = _currentId; i > 0; i--)
            UnregisterHotKey(_window.Handle, i);   
    }

    public void Dispose()
    {
        unregister();
       
   
        _window.Dispose();
    }

    #endregion
}
 

}
