using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication4
{
    public partial class CustomControl1 : ComboBox
    {
        public CustomControl1()
        {
            InitializeComponent();
        }

        public CustomControl1 Peer { get; set; }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            int id = e.Index;
            if (checkID(id))
            {
                e.Graphics.FillRectangle(SystemBrushes.Window, e.Bounds);
                e.Graphics.DrawString(Items[id].ToString(), Font, Brushes.LightSlateGray, e.Bounds);
            }
            else
            {
                e.DrawBackground();
                e.Graphics.DrawString(Items[id].ToString(), Font, ((e.State & DrawItemState.Selected) > 0) ? SystemBrushes.HighlightText : SystemBrushes.ControlText, e.Bounds);
                e.DrawFocusRectangle();
            }
        }
        
      protected override void OnSelectedIndexChanged(EventArgs e)
        {
            if (checkID(SelectedIndex))

                SelectedIndex = -1;

            else
            {
                base.OnSelectedIndexChanged(e);
                if (Peer != null)
                {
                    Peer.Tag = SelectedIndex;
                    Peer.Refresh();
                }
            }
        }

        private Boolean checkID(int id)
        {
            return Tag is int ? id>0&&id == (int)Tag : false;
        }
    }
}
