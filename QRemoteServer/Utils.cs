using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace QRemoteServer
{
    class Utils
    {
        public static Image ImageFromScreen(bool withCursor)
        {
            Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height);
            using (var gr = Graphics.FromImage(bmp))
            {
                gr.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y,
                    0, 0, Screen.PrimaryScreen.Bounds.Size);
                if (withCursor)
                {
                    if (Cursor.Current != null)
                        using (Icon cursor = Icon.FromHandle(Cursors.Default.Handle))
                            gr.DrawIcon(cursor, new Rectangle(Cursor.Position, cursor.Size));
                }
            }
            return bmp;
        }

        public static byte[] ImageToByte(Image img)
        {
            byte[] byteArray = new byte[10000];
            MemoryStream stream = new MemoryStream();
            img.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
            byteArray = stream.ToArray();
            stream.Close();
            stream.Dispose();
            return byteArray;
        }
        public static Image byteArrayToImage(byte[] byteArrayIn)
        {
            MemoryStream ms = new MemoryStream(byteArrayIn);
            Image returnImage = Image.FromStream(ms);
            ms.Dispose();
            return returnImage;
        }
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }
}
