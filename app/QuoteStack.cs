// QuoteStack P3 skeleton: always-on-top floating bar + Alt+Q capture from Edge.
// Single file, .NET Framework WinForms, no third-party dependencies.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace QuoteStack
{
    // ---------- 主题令牌（P1）----------
    // 换皮只改这里；像素由各控件的 OnPaint 使用这些角色色。
    static class Theme
    {
        internal static bool IsLight;

        internal static Color TextPrimary, TextBody, TextMuted, TextMutedSoft, TextDisabled, TextSoft;
        internal static Color BarBg, PanelBg, FieldBg, RowActive, RowIdle, RowHover, DangerField, ToolBg, DeepBg;
        internal static Color Warn, Danger, FlashOk, FlashErr, BorderBar;
        internal static Color BottomBar, ScrollThumb, OrdinalText, UrlText, PreviewText, HintText, GroupLabel, RowText, BarHint, SettingsText, SettingsMuted, DangerText, GearRing, SettingsBorder, SettingsArrow;

        static Theme() { Apply(false); }

        // 渐变高光边：浅色走白→淡粉，深色走白 70 → 几乎透明，1.2px。
        // 圆角路径（内容板用）：四段弧，半径按较小边收敛。
        // 只圆下方两角（底部栏用）：上方保持直角，和列表内容无缝相接。
        internal static GraphicsPath BottomRounded(RectangleF r, float radius)
        {
            var p = new GraphicsPath();
            float d = Math.Max(2f, Math.Min(radius * 2f, Math.Min(r.Width, r.Height)));
            p.AddLine(r.X, r.Y, r.Right, r.Y);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
        // 用窗口 Region 把四角挖圆：不依赖 DWM，任何 Win10/11 都生效（边缘无抗锯齿）。
        internal static void RoundRegion(Form f, float radius)
        {
            if (f == null || f.IsDisposed) return;
            if (f.Region != null) { Region r0 = f.Region; f.Region = null; r0.Dispose(); }
            int hr = Native.DwmRound(f.Handle);
            Store.LogStartup("corner: dwm=" + hr + " radius=" + (int)radius);
        }

        internal static GraphicsPath RoundPath(RectangleF r, float radius)
        {
            var p = new GraphicsPath();
            float d = Math.Max(2f, Math.Min(radius * 2f, Math.Min(r.Width, r.Height)));
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        internal static void DrawRim(Graphics g, GraphicsPath path, Rectangle r)
        {
            Color a = IsLight ? Color.FromArgb(235, 255, 255, 255) : Color.FromArgb(70, 255, 255, 255);
            Color b = IsLight ? Color.FromArgb(140, 255, 198, 216) : Color.FromArgb(16, 255, 255, 255);
            var box = new Rectangle(r.X, r.Y, Math.Max(2, r.Width), Math.Max(2, r.Height));
            using (var lg = new LinearGradientBrush(box, a, b, 55f))
            using (var pen = new Pen(lg, 1.2f))
                g.DrawPath(pen, path);
        }
        internal static bool SystemPrefersLight()
        {
            try
            {
                object v = Microsoft.Win32.Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "AppsUseLightTheme", null);
                return (v is int) && ((int)v != 0);
            }
            catch { return false; }   // 读不到就按深色（= 改造前的观感）
        }

        internal static void Apply(bool light)
        {
            IsLight = light;
            if (light)
            {
                TextPrimary   = Color.FromArgb(38, 42, 52);
                TextBody      = Color.FromArgb(58, 65, 80);
                TextMuted     = Color.FromArgb(110, 118, 136);
                TextMutedSoft = Color.FromArgb(112, 120, 138);
                TextDisabled  = Color.FromArgb(154, 161, 174);
                TextSoft      = Color.FromArgb(122, 130, 150);

                BarBg       = Color.FromArgb(246, 247, 251);
                PanelBg     = Color.FromArgb(250, 251, 255);
                FieldBg     = Color.FromArgb(243, 245, 250);
                RowActive   = Color.FromArgb(247, 236, 241);
                RowIdle     = Color.FromArgb(252, 252, 254);
                RowHover    = Color.FromArgb(232, 236, 244);
                DangerField = Color.FromArgb(253, 235, 235);
                ToolBg      = Color.FromArgb(238, 241, 246);
                DeepBg      = Color.FromArgb(240, 242, 247);

                Warn     = Color.FromArgb(201, 122, 34);
                Danger   = Color.FromArgb(214, 88, 122);
                FlashOk  = Color.FromArgb(226, 236, 252);
                FlashErr = Color.FromArgb(252, 232, 232);
                BorderBar = Color.FromArgb(120, 134, 160, 80);
            }
            else
            {
                TextPrimary   = Color.FromArgb(228, 231, 238);
                TextBody      = Color.FromArgb(196, 202, 214);
                TextMuted     = Color.FromArgb(150, 156, 168);
                TextMutedSoft = Color.FromArgb(150, 156, 170);
                TextDisabled  = Color.FromArgb(96, 100, 112);
                TextSoft      = Color.FromArgb(120, 126, 140);

                BarBg       = Color.FromArgb(28, 28, 30);
                PanelBg     = Color.FromArgb(28, 28, 32);
                FieldBg     = Color.FromArgb(40, 42, 48);
                RowActive   = Color.FromArgb(44, 46, 54);
                RowIdle     = Color.FromArgb(30, 30, 34);
                RowHover    = Color.FromArgb(60, 64, 76);
                DangerField = Color.FromArgb(52, 40, 40);
                ToolBg      = Color.FromArgb(52, 54, 62);
                DeepBg      = Color.FromArgb(24, 24, 26);

                Warn     = Color.FromArgb(240, 170, 110);
                Danger   = Color.FromArgb(255, 150, 140);
                FlashOk  = Color.FromArgb(38, 52, 92);
                FlashErr = Color.FromArgb(72, 34, 34);
                BorderBar = Color.FromArgb(46, 255, 255, 255);
            }

            // 第三批：零散文本与线条。浅色下这些必须换成深墨色，否则白底上全是灰字。
            PreviewText    = light ? Color.FromArgb(90, 96, 110)   : Color.FromArgb(168, 168, 176);
            HintText       = light ? Color.FromArgb(40, 92, 200)   : Color.FromArgb(120, 160, 255);
            GroupLabel     = light ? Color.FromArgb(120, 128, 142) : Color.FromArgb(126, 132, 146);
            RowText        = light ? Color.FromArgb(42, 46, 56)    : Color.FromArgb(232, 232, 238);
            BarHint        = light ? Color.FromArgb(110, 118, 132) : Color.FromArgb(140, 146, 160);
            SettingsText   = light ? Color.FromArgb(38, 42, 52)    : Color.FromArgb(230, 233, 240);
            SettingsMuted  = light ? Color.FromArgb(110, 118, 136) : Color.FromArgb(140, 146, 158);
            DangerText     = light ? Color.FromArgb(176, 44, 66)   : Color.FromArgb(255, 200, 190);
            GearRing       = light ? Color.FromArgb(150, 158, 172) : Color.FromArgb(96, 102, 118);
            SettingsBorder = light ? Color.FromArgb(210, 215, 224) : Color.FromArgb(60, 62, 72);
            SettingsArrow  = light ? Color.FromArgb(122, 130, 146) : Color.FromArgb(160, 166, 178);
            OrdinalText    = light ? Color.FromArgb(22, 120, 80)   : Color.FromArgb(106, 227, 160);
            UrlText        = light ? Color.FromArgb(40, 96, 200)   : Color.FromArgb(96, 148, 255);
            BottomBar      = light ? Color.FromArgb(243, 246, 251) : Color.FromArgb(34, 34, 38);
            ScrollThumb    = light ? Color.FromArgb(200, 206, 216) : Color.FromArgb(58, 60, 68);
        }
    }
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // 先定主题：下面所有控件的构造都读 Theme.*，晚一步就会用错配色。
            Theme.Apply(Theme.SystemPrefersLight());
            // Runs before every mode: a temp file left by an interrupted delete is
            // never valid data and must not survive a restart.
            Store.CleanupTemp();
            foreach (string a in args)
            {
                // Reports the facts that decide whether SendInput can work at all:
                // this process's integrity level, the foreground window's level, and
                // the INPUT struct size. Nothing about page content is read.
                if (a == "--diag")
                {
                    IntPtr fg = Native.GetForegroundWindow();
                    Store.LogStartup("diag: self=" + Native.IntegrityLabel(Native.GetCurrentProcess())
                        + " fg=" + Native.ProcessNameOf(fg)
                        + " fgIL=" + Native.WindowIntegrity(fg)
                        + " inputSize=" + Native.InputSize()
                        + " dataDir=" + Store.DirPath);
                    Environment.Exit(0);
                    return;
                }
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FloatBar());
        }
    }

    static class Store
    {
        static readonly object Gate = new object();
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
        public static string DirPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "quote-stack");
            }
        }

        public static string FilePath
        {
            get { return Path.Combine(DirPath, "stack.ndjson"); }
        }

        public static int Count()
        {
            try
            {
                if (!File.Exists(FilePath)) return 0;
                int n = 0;
                using (var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Utf8))
                {
                    while (sr.ReadLine() != null) n++;
                }
                return n;
            }
            catch { return -1; }
        }

        public static string LastText()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                string last = null;
                using (var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Utf8))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                        if (line.Trim().Length > 0) last = line;
                }
                if (last == null) return null;
                return JsonField(last, "text");
            }
            catch { return null; }
        }

        // The stack is a working set, not an archive: once it holds more than this many
        // records the oldest ones fall off the front.
        public const int MaxRecords = 500;

        // Captured images live next to the stack, one PNG per record, named after the
        // record id so a record and its image can never drift apart.
        public static string ImagesDir
        {
            get { return Path.Combine(DirPath, "images"); }
        }

        public static string ImagePath(string name)
        {
            if (name == null || name.Length == 0) return null;
            return Path.Combine(ImagesDir, name);
        }

        // Best effort: a picture nobody points at is dead weight, so removal never throws.
        public static bool DeleteImage(string name)
        {
            try
            {
                string p = ImagePath(name);
                if (p == null || !File.Exists(p)) return true;
                File.Delete(p);
                return true;
            }
            catch (Exception ex)
            {
                LogStartup("image: delete-failed " + ex.GetType().Name);
                return false;
            }
        }

        // One PNG, written to a temp file and then swapped in: a crash mid-write leaves
        // the temp file (removed at startup) and never a half image the list would show.
        public static bool SaveImagePng(byte[] png, string name, out string error)
        {
            error = null;
            if (png == null || png.Length == 0) { error = "empty"; return false; }
            try
            {
                Directory.CreateDirectory(ImagesDir);
                string dst = ImagePath(name);
                string tmp = dst + ".tmp";
                File.WriteAllBytes(tmp, png);
                ReplaceFileAtomic(tmp, dst);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.GetType().Name;
                return false;
            }
        }

        public static bool Append(string text, string source, string title, string url, string image, out string error)
        {
            error = null;
            var sb = new StringBuilder(256 + text.Length);
            // Written per record so two captures of the same sentence stay two different
            // records: deleting one no longer deletes the other (D1 in the 2026-10-28 audit).
            sb.Append("{\"id\":\"").Append(Guid.NewGuid().ToString("N").Substring(0, 12)).Append("\",");
            sb.Append("\"ts\":\"").Append(DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz")).Append("\",");
            sb.Append("\"source\":\"").Append(Escape(source)).Append("\",");
            sb.Append("\"title\":\"").Append(Escape(title)).Append("\",");
            sb.Append("\"url\":\"").Append(Escape(url == null ? "" : url)).Append("\",");
            if (image != null && image.Length > 0)
                sb.Append("\"image\":\"").Append(Escape(image)).Append("\",");
            sb.Append("\"text\":\"").Append(Escape(text)).Append("\"}");
            string line = sb.ToString();

            lock (Gate)
            {
                try
                {
                    Directory.CreateDirectory(DirPath);
                    byte[] bytes = Utf8.GetBytes(line + "\n");
                    using (var fs = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                    {
                        fs.Write(bytes, 0, bytes.Length);
                        fs.Flush();
                    }
                    TrimTo(MaxRecords);
                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.GetType().Name + ": " + ex.Message;
                    return false;
                }
            }
        }

        // Which quotes have already been inserted at least once. Kept in its own
        // journal so the capture file stays append-only: marking a record never
        // rewrites the stack, only deletion does.
        public static string UsedPath
        {
            get { return Path.Combine(DirPath, "used.ndjson"); }
        }

        // A crash between writing stack.ndjson.tmp and replacing the real file leaves the
        // temp file behind. It is never valid data on its own, so it is removed at startup
        // instead of piling up next to the real file.
        public static void CleanupTemp()
        {
            lock (Gate)
            {
                try { if (File.Exists(FilePath + ".tmp")) File.Delete(FilePath + ".tmp"); }
                catch { }
                try { if (File.Exists(UsedPath + ".tmp")) File.Delete(UsedPath + ".tmp"); }
                catch { }
            }
        }

        // 12 hex chars of SHA-256 over the exact text. Used as the identity of a quote
        // for the used-journal and for deletion, because the stack has no id column.
        public static string Hash12(string text)
        {
            if (text == null) return "";
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] h = sha.ComputeHash(Utf8.GetBytes(text));
                var sb = new StringBuilder(12);
                for (int i = 0; i < 6; i++) sb.Append(h[i].ToString("x2"));
                return sb.ToString();
            }
        }

        public static bool AppendUsed(string key)
        {
            if (key == null || key.Length == 0) return false;
            lock (Gate)
            {
                try
                {
                    Directory.CreateDirectory(DirPath);
                    string line = "{\"ts\":\"" + DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz")
                        + "\",\"key\":\"" + key + "\"}\n";
                    byte[] bytes = Utf8.GetBytes(line);
                    using (var fs = new FileStream(UsedPath, FileMode.Append, FileAccess.Write, FileShare.Read))
                    {
                        fs.Write(bytes, 0, bytes.Length);
                        fs.Flush();
                    }
                    return true;
                }
                catch { return false; }
            }
        }

        // Same-volume replace of dst with the fully written src, in one metadata
        // operation. File.Replace is not usable here: on this .NET it fails with
        // "路径的形式不合法" for a temp file in the same directory, and the delete would
        // then have to be two steps (delete + rename), which can lose the stack if it
        // is interrupted. Red line 9 wants temp-file -> replace, so use the primitive
        // that actually does it.
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool MoveFileExW(string existingFileName, string newFileName, int flags);

        internal static bool ReplaceFileAtomic(string src, string dst)
        {
            const int MOVEFILE_REPLACE_EXISTING = 0x1;
            const int MOVEFILE_WRITE_THROUGH = 0x8;
            return MoveFileExW(src, dst, MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH);
        }

        public static HashSet<string> LoadUsed()        {
            HashSet<string> set = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                if (!File.Exists(UsedPath)) return set;
                using (var fs = new FileStream(UsedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Utf8))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        // New journals store the record id under "key"; v1 wrote a text hash
                        // under "sha12". Both are read so an old journal keeps working.
                        string h = JsonField(line, "key");
                        if (h == null) h = JsonField(line, "sha12");
                        if (h != null && h.Length > 0) set.Add(h);
                    }
                }
            }
            catch { }
            return set;
        }

        // Removes every record whose identity is in drop. Identity is the per-record id
        // written at capture time; v1 rows have no id and fall back to "h:" + text hash,
        // which is what the old code deleted by. The rewrite goes to a temp file in the
        // same directory and then MoveFileExW replaces it, so an interrupted delete cannot
        // leave a half-written stack (red line 9: no non-atomic writes).
        // A record that is removed must not leave its picture behind: the PNG is named
        // after the record, so the line alone is enough to find it. Best effort — a picture
        // that cannot be deleted is not a reason to fail the delete.
        static void DeleteImageOf(string line)
        {
            try
            {
                string img = JsonField(line, "image");
                if (img == null || img.Length == 0) return;
                DeleteImage(img);
            }
            catch (Exception ex) { LogStartup("image: delete-failed " + ex.GetType().Name); }
        }

        public static bool DeleteByKey(HashSet<string> drop, out int removed, out string error)
        {
            removed = 0;
            error = null;
            if (drop == null || drop.Count == 0) return true;
            lock (Gate)
            {
                string tmp = null;
                try
                {
                    Directory.CreateDirectory(DirPath);
                    if (!File.Exists(FilePath)) return true;
                    List<string> keep = new List<string>();
                    using (var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs, Utf8))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (line.Trim().Length == 0) continue;
                            string text = JsonField(line, "text");
                            if (text == null) continue;
                            string id = JsonField(line, "id");
                            string key = (id != null && id.Length > 0) ? id : ("h:" + Hash12(text));
                            if (drop.Contains(key)) { removed++; DeleteImageOf(line); continue; }
                            keep.Add(line);
                        }
                    }
                    if (removed == 0) return true;
                    tmp = FilePath + ".tmp";
                    using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        foreach (string l in keep)
                        {
                            byte[] b = Utf8.GetBytes(l + "\n");
                            fs.Write(b, 0, b.Length);
                        }
                        fs.Flush(true);
                    }
                    if (!ReplaceFileAtomic(tmp, FilePath))
                        throw new IOException("atomic-replace-failed err=" + Marshal.GetLastWin32Error());
                    tmp = null;
                    PruneUsed(drop);
                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.GetType().Name + ": " + ex.Message;
                    if (tmp != null) { try { File.Delete(tmp); } catch { } }
                    return false;
                }
            }
        }

        // Keeps the newest max records and drops the oldest ones. Called after every append,
        // so the stack can never grow past MaxRecords. The rewrite is the same atomic
        // replace as deletion; if it cannot be done the extra records stay until the next
        // capture rather than being lost.
        public static int TrimTo(int max)
        {
            int dropped = 0;
            if (max <= 0) return 0;
            lock (Gate)
            {
                string tmp = null;
                try
                {
                    if (!File.Exists(FilePath)) return 0;
                    List<string> lines = new List<string>();
                    using (var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs, Utf8))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                            if (line.Trim().Length > 0) lines.Add(line);
                    }
                    int cut = lines.Count - max;
                    if (cut <= 0) return 0;
                    HashSet<string> gone = new HashSet<string>(StringComparer.Ordinal);
                    for (int i = 0; i < cut; i++)
                    {
                        string text = JsonField(lines[i], "text");
                        if (text == null) continue;
                        string id = JsonField(lines[i], "id");
                        gone.Add((id != null && id.Length > 0) ? id : ("h:" + Hash12(text)));
                        DeleteImageOf(lines[i]);
                    }
                    List<string> keep = lines.GetRange(cut, lines.Count - cut);
                    tmp = FilePath + ".tmp";
                    using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        foreach (string l in keep)
                        {
                            byte[] b = Utf8.GetBytes(l + "\n");
                            fs.Write(b, 0, b.Length);
                        }
                        fs.Flush(true);
                    }
                    if (!ReplaceFileAtomic(tmp, FilePath))
                        throw new IOException("atomic-replace-failed err=" + Marshal.GetLastWin32Error());
                    tmp = null;
                    PruneUsed(gone);
                    dropped = cut;
                    LogStartup("trim: dropped=" + cut + " kept=" + max); // counts only
                }
                catch (Exception ex)
                {
                    if (tmp != null) { try { File.Delete(tmp); } catch { } }
                    dropped = 0;
                    LogStartup("trim: failed " + ex.GetType().Name); // no text in the log
                }
                return dropped;
            }
        }

        static void PruneUsed(HashSet<string> drop)
        {
            string tmp = null;
            try
            {
                if (!File.Exists(UsedPath)) return;
                List<string> keep = new List<string>();
                using (var fs = new FileStream(UsedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Utf8))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Trim().Length == 0) continue;
                        string h = JsonField(line, "key");
                        if (h == null) h = JsonField(line, "sha12");
                        if (h != null && (drop.Contains(h) || drop.Contains("h:" + h))) continue;
                        keep.Add(line);
                    }
                }
                tmp = UsedPath + ".tmp";
                using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    foreach (string l in keep)
                    {
                        byte[] b = Utf8.GetBytes(l + "\n");
                        fs.Write(b, 0, b.Length);
                    }
                    fs.Flush(true);
                }
                if (!ReplaceFileAtomic(tmp, UsedPath))
                    throw new IOException("atomic-replace-failed err=" + Marshal.GetLastWin32Error());
                tmp = null;
            }
            catch
            {
                if (tmp != null) { try { File.Delete(tmp); } catch { } }
            }
        }

        // Startup decisions are appended here so a failed launch is diagnosable
        // without a debugger. Only config/hotkey outcomes are recorded, never
        // captured text.
        public static void LogStartup(string message)
        {
            try
            {
                Directory.CreateDirectory(DirPath);
                RotateLogIfNeeded();
                string line = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss") + " " + message + "\n";
                using (var fs = new FileStream(Path.Combine(DirPath, "startup.log"), FileMode.Append, FileAccess.Write, FileShare.Read))
                using (var sw = new StreamWriter(fs, Utf8))
                {
                    sw.Write(line);
                    sw.Flush();
                }
            }
            catch { }
        }

        // The log is append-only and written on every capture, so without a cap it grows forever.
        // Past 512 KB it is renamed to startup.log.1 (one generation kept) and a fresh log starts.
        // A rename inside one directory is atomic, so a crash here leaves either the old file or
        // the rotated one, never a half file.
        public const long LogLimitBytes = 512 * 1024;

        public static void RotateLogIfNeeded()
        {
            try
            {
                string path = LogPath;
                var fi = new FileInfo(path);
                if (!fi.Exists || fi.Length <= LogLimitBytes) return;
                string old = path + ".1";
                if (File.Exists(old)) File.Delete(old);
                File.Move(path, old);
            }
            catch { }
        }

        public static string LogPath
        {
            get { return Path.Combine(DirPath, "startup.log"); }
        }

        // Length of the log right now. Used as a watermark: whatever a replacement instance
        // writes after this offset is the only evidence about it that counts.
        public static long LogLength()
        {
            try
            {
                FileInfo fi = new FileInfo(LogPath);
                return fi.Exists ? fi.Length : 0;
            }
            catch { return 0; }
        }

        // True when a marker appears in the bytes appended after `offset`. Reading only the tail
        // keeps this cheap even when the log has grown for weeks.
        public static bool LogHasAfter(long offset, string marker, int maxBytes)
        {
            try
            {
                if (!File.Exists(LogPath)) return false;
                using (FileStream fs = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long len = fs.Length;
                    if (len <= offset) return false;
                    long start = Math.Max(offset, len - maxBytes);
                    fs.Seek(start, SeekOrigin.Begin);
                    byte[] buf = new byte[len - start];
                    int read = fs.Read(buf, 0, buf.Length);
                    string text = Encoding.UTF8.GetString(buf, 0, read);
                    return text.Contains(marker);
                }
            }
            catch { return false; }
        }

        // Writes one fixed record into the production file, then exits.
        // Used only by manual verification; it is tagged source=selftest so it is
        // distinguishable from real captures.
        public static int SelfTest()
        {
            string err;
            bool ok = Append("SELFTEST-RECORD 这条记录由 --selftest 写入，用于验证存储路径与追加写入。", "selftest", "selftest", "", null, out err);
            return ok ? 0 : 1;
        }

        public static string Escape(string s)
        {
            if (s == null) return "";
            var sb = new StringBuilder(s.Length + 16);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        // Minimal extraction of one string field; this is not a general JSON parser.
        public static string JsonField(string json, string key)
        {
            string needle = "\"" + key + "\":\"";
            int i = json.IndexOf(needle, StringComparison.Ordinal);
            if (i < 0) return null;
            i += needle.Length;
            var sb = new StringBuilder();
            while (i < json.Length)
            {
                char c = json[i];
                if (c == '\\' && i + 1 < json.Length)
                {
                    char n = json[i + 1];
                    switch (n)
                    {
                        case 'r': sb.Append('\r'); i += 2; continue;
                        case 'n': sb.Append('\n'); i += 2; continue;
                        case 't': sb.Append('\t'); i += 2; continue;
                        case '"': sb.Append('"'); i += 2; continue;
                        case '\\': sb.Append('\\'); i += 2; continue;
                        case 'u':
                            if (i + 5 < json.Length)
                            {
                                sb.Append((char)Convert.ToInt32(json.Substring(i + 2, 4), 16));
                                i += 6;
                                continue;
                            }
                            break;
                    }
                    sb.Append(n); i += 2; continue;
                }
                if (c == '"') return sb.ToString();
                sb.Append(c); i++;
            }
            return sb.ToString();
        }
    }

    static class Config
    {
        public static string FilePath
        {
            get { return Path.Combine(Store.DirPath, "config.json"); }
        }

        // Creates the file with the documented default on first run.
        public static string Ensure()
        {
            try
            {
                Directory.CreateDirectory(Store.DirPath);
                if (!File.Exists(FilePath)) WriteText("{\"hotkey\":\"Alt+Q\"}");
            }
            catch { }
            return FilePath;
        }

        public static string ReadHotkey()
        {
            try
            {
                if (!File.Exists(FilePath)) return "Alt+Q";
                string json = File.ReadAllText(FilePath, Encoding.UTF8);
                string v = Store.JsonField(json, "hotkey");
                return v;
            }
            catch { return null; }
        }

        // Remembered geometry: the drawer reopens at the size the user last left it at.
        // Stored in the same config.json as the hotkey, so uninstall still means one folder.
        // Values below this floor are ignored as corrupt input.
        public static bool ReadDrawerSize(out int w, out int h)
        {
            w = 0;
            h = 0;
            try
            {
                if (!File.Exists(FilePath)) return false;
                string json = File.ReadAllText(FilePath, Encoding.UTF8);
                int pw, ph;
                if (!int.TryParse(Store.JsonField(json, "drawerW"), out pw)) return false;
                if (!int.TryParse(Store.JsonField(json, "drawerH"), out ph)) return false;
                if (pw < 200 || ph < 200) return false;
                w = pw;
                h = ph;
                return true;
            }
            catch { return false; }
        }

        // Rewrites config.json keeping the hotkey text verbatim and updating only the geometry.
        public static void SaveDrawerSize(int w, int h)
        {
            try
            {
                string hk = ReadHotkey();
                if (hk == null || hk.Length == 0) hk = "Alt+Q";
                WriteText("{\"hotkey\":\"" + Store.Escape(hk) + "\",\"drawerW\":\"" + w
                    + "\",\"drawerH\":\"" + h + "\"}");
            }
            catch (Exception ex)
            {
                Store.LogStartup("config: save-drawer failed " + ex.GetType().Name);
            }
        }

        // Rewrites config.json keeping the remembered geometry and replacing only the hotkey.
        // Called from the settings panel, which is why editing the file by hand is no longer
        // the only way to change the key.
        public static void SaveHotkey(string hk)
        {
            int w, h;
            bool haveSize = ReadDrawerSize(out w, out h);
            string json = "{\"hotkey\":\"" + Store.Escape(hk) + "\"";
            if (haveSize) json += ",\"drawerW\":\"" + w + "\",\"drawerH\":\"" + h + "\"";
            json += "}";
            WriteText(json);
        }

        // Atomic replace: temp file first, then swap, so a crash cannot leave a
        // half-written config behind.
        public static void WriteText(string content)
        {
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, content, new UTF8Encoding(false));
            if (File.Exists(FilePath))
            {
                if (!Store.ReplaceFileAtomic(tmp, FilePath))
                {
                    // Fallback when the metadata swap is refused: this path is not atomic,
                    // so it is logged instead of being silent.
                    Store.LogStartup("config: atomic-replace-failed err=" + Marshal.GetLastWin32Error());
                    File.Delete(FilePath);
                    File.Move(tmp, FilePath);
                }
            }
            else
            {
                File.Move(tmp, FilePath);
            }
        }
    }

    static class Hotkey
    {
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CTRL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        // Accepts forms like "Alt+Q", "Ctrl+Alt+Q", "Alt+Shift+F2".
        // Returns false with a human readable reason when the text cannot be used.
        public static bool Parse(string text, out uint mods, out uint vk, out string error)
        {
            mods = 0;
            vk = 0;
            error = null;
            if (text == null || text.Trim().Length == 0)
            {
                error = "hotkey 为空";
                return false;
            }

            string[] parts = text.Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                error = "至少需要一个修饰键和一个主键，例如 Alt+Q";
                return false;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i].Trim().ToUpperInvariant();
                bool isKey = i == parts.Length - 1;
                if (isKey)
                {
                    uint k;
                    if (!ParseKey(p, out k))
                    {
                        error = "不认识的主键：" + p + "（支持 A-Z、0-9、F1-F24）";
                        return false;
                    }
                    vk = k;
                }
                else
                {
                    switch (p)
                    {
                        case "ALT": mods |= MOD_ALT; break;
                        case "CTRL":
                        case "CONTROL": mods |= MOD_CTRL; break;
                        case "SHIFT": mods |= MOD_SHIFT; break;
                        case "WIN": mods |= MOD_WIN; break;
                        default:
                            error = "不认识的修饰键：" + p + "（支持 Alt、Ctrl、Shift、Win）";
                            return false;
                    }
                }
            }

            if (mods == 0)
            {
                error = "不允许没有修饰键的单键热键";
                return false;
            }
            if (mods == MOD_WIN)
            {
                error = "不允许单独使用 Win 组合";
                return false;
            }
            return true;
        }

        // Turns a captured key press into the same text form Parse() accepts, so the settings panel
        // can record a chord instead of asking the user to type it. Kept separate from the control
        // so it can be exercised without a window.
        public static bool FromKeys(Keys keyCode, Keys modifiers, out string text, out string error)
        {
            text = null;
            error = null;
            if (keyCode == Keys.None)
            {
                error = "没有识别到按键";
                return false;
            }
            string name = KeyName(keyCode);
            if (name == null)
            {
                error = "这个键暂不支持（只用 A-Z、0-9、F1-F24）";
                return false;
            }
            string mods = "";
            if ((modifiers & Keys.Control) != 0) mods += "Ctrl+";
            if ((modifiers & Keys.Alt) != 0) mods += "Alt+";
            if ((modifiers & Keys.Shift) != 0) mods += "Shift+";
            if (mods.Length == 0)
            {
                error = "需要至少一个修饰键（Ctrl / Shift / Alt）";
                return false;
            }
            text = mods + name;
            return true;
        }

        public static string KeyName(Keys k)
        {
            int v = (int)k;
            if (v >= (int)Keys.A && v <= (int)Keys.Z) return ((char)('A' + (v - (int)Keys.A))).ToString();
            if (v >= (int)Keys.D0 && v <= (int)Keys.D9) return ((char)('0' + (v - (int)Keys.D0))).ToString();
            if (v >= (int)Keys.F1 && v <= (int)Keys.F24) return "F" + (v - (int)Keys.F1 + 1);
            return null;
        }

        static bool ParseKey(string p, out uint vk)
        {
            vk = 0;
            if (p.Length == 1)
            {
                char c = p[0];
                if (c >= 'A' && c <= 'Z') { vk = c; return true; }
                if (c >= '0' && c <= '9') { vk = c; return true; }
                return false;
            }
            if (p.Length >= 2 && p[0] == 'F')
            {
                int n;
                if (int.TryParse(p.Substring(1), out n) && n >= 1 && n <= 24)
                {
                    vk = (uint)(0x70 + n - 1);
                    return true;
                }
            }
            return false;
        }
    }

    class CaptureResult
    {
        public bool Ok;
        public bool HadSelection;
        public string Message;
        public string Preview;
    }

    static class Capture
    {
        const int PollMs = 900;

        // Sequence number of the clipboard as this program last left it. A picture copied with
        // the browser's context menu sits on the clipboard before the hotkey is pressed, so the
        // capture path accepts it — but only once, never again on a second press of the same key.
        static uint LastSeenClipSeq;

        // A URL is stored only when the clipboard text really is one: scheme, no
        // whitespace, no newline, sane length. Anything else is treated as "no URL"
        // instead of writing the address bar's placeholder or a stray fragment.
        public static bool LooksLikeUrl(string s)
        {
            if (s == null) return false;
            s = s.Trim();
            if (s.Length < 10 || s.Length > 2048) return false;
            if (s.IndexOfAny(new char[] { '\r', '\n', ' ', '\t' }) >= 0) return false;
            return s.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        public static CaptureResult Run()
        {
            IntPtr hwnd = Native.GetForegroundWindow();
            string proc = Native.ProcessNameOf(hwnd);
            if (FloatBar.IsTerminal(proc))
            {
                // Plain Ctrl+C is "interrupt" in a shell, not "copy": refuse instead of
                // killing whatever the user is running. The drawer opens instead (see WndProc).
                Store.LogStartup("capture-refused: fg=" + proc + " reason=terminal");
                return new CaptureResult { Ok = false, HadSelection = true, Message = "终端里不采集（Ctrl+C 会中断命令）" };
            }

            string title = Native.TitleOf(hwnd);

            Native.ClipBackup backup;
            int formats;
            uint seq0;
            bool hadSnapshot = Native.ClipboardSnapshot(out backup, out formats, out seq0);
            if (!hadSnapshot)
            {
                Store.LogStartup("capture-refused: fg=" + proc + " reason=clipboard-busy");
                return new CaptureResult { Ok = false, HadSelection = true, Message = "剪贴板被占用" };
            }

            Stopwatch sw = Stopwatch.StartNew();

            // A picture is normally copied with the browser's right-click menu ("复制图片"),
            // which puts it on the clipboard BEFORE the hotkey is pressed — Ctrl+C in the page
            // then copies nothing, so waiting for a clipboard change would never see it.
            // Reading one picture at the start of every capture is cheap (~40ms) and only
            // accepted when the clipboard changed since the previous capture, so the same
            // picture cannot be stored twice by pressing the key again.
            byte[] pendingImg = null;
            string pendingWhy = "skip";
            if (seq0 != LastSeenClipSeq)
            {
                pendingImg = Native.ReadClipImagePng(Native.MaxImageBytes);
                pendingWhy = pendingImg == null ? "none" : "ok";
            }

            string heldBefore = Native.HeldModifiers();
            long waitedMods = Native.WaitModifiersReleased(1200);
            string heldAfterWait = Native.HeldModifiers();
            int released = 0;
            if (!heldAfterWait.Equals("-"))
            {
                // The user is still holding a modifier: release it before typing a chord,
                // otherwise the injected C arrives as Alt+Shift+Ctrl+C and copies nothing.
                released = Native.ForceReleaseModifiers();
                Thread.Sleep(40);
            }
            string heldAtSend = Native.HeldModifiers();

            // The wait above gives the user time to switch windows, so the target is
            // checked again immediately before any key is injected (T3 constraint:
            // never type into an unverified foreground window).
            string procNow = Native.ProcessNameOf(Native.GetForegroundWindow());
            if (!string.Equals(procNow, proc, StringComparison.OrdinalIgnoreCase))
            {
                Store.LogStartup("capture-refused: target changed during wait, fg=" + procNow);
                Native.ClipboardRestore(backup);
                return new CaptureResult { Ok = false, HadSelection = true, Message = "窗口已切换" };
            }

            int sent = Native.SendCtrlC(25);
            uint seq = Native.WaitClipboardChange(seq0, 300);
            string via = "ctrl-c";
            int sent2 = 0;
            if (seq == seq0)
            {
                // Only used to tell "there is a selection but this page ignores Ctrl+C" from
                // "there is no selection at all". Short cap: this path is also the insert
                // branch, where the user is waiting for the drawer.
                sent2 = Native.SendCtrlInsert(25);
                uint seq2 = Native.WaitClipboardChange(seq0, 180);
                if (seq2 != seq0) { seq = seq2; via = "ctrl-insert"; }
                else via = "none";
            }
            long waited = sw.ElapsedMilliseconds;

            string text = null;
            if (seq != seq0) text = Native.ReadClipText(6);

            // A picture copied from a page carries no text; a mixed selection carries both.
            // The PNG is written before the record so a record never points at a file that
            // was not saved. An oversized image is dropped, not truncated.
            byte[] imgPng = null;
            string imgName = null;
            string imgWhy = "skip";
            if (seq != seq0)
            {
                imgPng = Native.ReadClipImagePng(Native.MaxImageBytes);
                imgWhy = imgPng == null ? "none" : "ok";
            }
            else if (pendingImg != null)
            {
                // No selection in the page, but a picture landed on the clipboard just before
                // the hotkey was pressed (right-click → 复制图片). That picture is the quote the
                // user meant, so store it instead of opening the drawer (rule m04274).
                imgPng = pendingImg;
                imgWhy = "from-clipboard";
                via = "clipboard-image";
            }
            if (imgPng != null)
            {
                imgName = Guid.NewGuid().ToString("N").Substring(0, 12) + ".png";
                string imgErr;
                if (!Store.SaveImagePng(imgPng, imgName, out imgErr))
                {
                    Store.LogStartup("image: save-failed " + imgErr);
                    imgName = null;
                }
            }

            // The address bar is read only after the selection has been copied: Ctrl+L moves
            // focus to the URL bar, so anything sent afterwards would copy the URL instead of
            // the page text. A URL is stored only when it really looks like one.
            string url = null;
            int sentL = 0;
            int urlRawLen = -1;
            string urlStep = "skip";
            // Pictures get the address too: an image copied from a page has no text, and the
            // "no URL on images" hole was reported as "图片引入不显示引入地址" (m04356).
            if (((text != null && text.Trim().Length > 0) || imgName != null) && FloatBar.IsEdgeProc(proc))
            {
                urlStep = "unchanged";
                // Two attempts: Ctrl+L focuses the address bar in Chromium, Alt+D is the
                // equivalent alias that some builds/versions still honour when Ctrl+L is
                // swallowed by the page. The step name is logged either way, so a failure
                // says which link broke instead of a bare "no url".
                for (int attempt = 0; attempt < 2 && url == null; attempt++)
                {
                    uint seqUrl0 = Native.Seq();
                    sentL += attempt == 0 ? Native.SendCtrlL(25) : Native.SendAltD(25);
                    Thread.Sleep(90);
                    Native.SendCtrlC(25);
                    uint seqUrl = Native.WaitClipboardChange(seqUrl0, 320);
                    if (seqUrl == seqUrl0) continue;
                    string candidate = Native.ReadClipText(6);
                    urlRawLen = candidate == null ? -1 : candidate.Length;
                    if (LooksLikeUrl(candidate))
                    {
                        url = candidate.Trim();
                        urlStep = "ok";
                    }
                    else
                    {
                        urlStep = "noturl";
                    }
                }
                Native.SendEscape(25);
            }

            Native.ClipboardRestore(backup);
            // Remembered so the same picture is not stored twice: the restore puts the original
            // clipboard back, so its sequence number is the one to compare against next time.
            LastSeenClipSeq = Native.Seq();
            // A missing picture has two very different causes: the page copied nothing (no
            // selection) or it copied something that is not a bitmap (a file, a link, HTML).
            // Format names — never their contents — say which one it was.
            string fmts = (imgPng == null && (seq != seq0 || pendingWhy == "none")) ? Native.ClipFormatNames(8) : "-";
            // This line exists because "clipboard unchanged" has two very different causes:
            // injection swallowed by UIPI, or injection landing but copying nothing.
            Store.LogStartup("capture: fg=" + proc
                + " self=" + Native.IntegrityLabel(Native.GetCurrentProcess())
                + " fgIL=" + Native.WindowIntegrity(hwnd)
                + " inputSize=" + Native.InputSize()
                + " held=" + heldBefore + "->" + heldAtSend
                + " waitedMods=" + waitedMods
                + " released=" + released
                + " sent=" + sent + "+" + sent2
                + " sentL=" + sentL
                + " via=" + via
                + " seq=" + seq0 + "->" + seq
                + " waited=" + waited
                + " backupLen=" + (backup == null || backup.Text == null ? -1 : backup.Text.Length)
                + " urlStep=" + urlStep
                + " urlRawLen=" + urlRawLen
                + " urlLen=" + (url == null ? -1 : url.Length)
                + " imgLen=" + (imgPng == null ? -1 : imgPng.Length)
                + " imgWhy=" + imgWhy
                + " imgPre=" + pendingWhy
                + " clipFmts=" + fmts
                + " textLen=" + (text == null ? -1 : text.Length));

            // No clipboard change means no selection: the same hotkey that stores a quote
            // must then open the drawer and insert at the caret instead (user rule m01388).
            bool hasText = text != null && text.Trim().Length > 0;
            if (!hasText && imgName == null)
                return new CaptureResult { Ok = false, HadSelection = false, Message = null };

            string err;
            if (!Store.Append(hasText ? text : "", proc, title, url, imgName, out err))
                return new CaptureResult { Ok = false, HadSelection = true, Message = "写入失败" };

            return new CaptureResult { Ok = true, HadSelection = true, Preview = hasText ? text : "（图片）" };
        }
    }

    class FloatBar : Form
    {
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.RoundRegion(this, 8f);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Theme.RoundRegion(this, 8f);
        }

        const int HOTKEY_ID = 9001;
        const uint MOD_ALT = 0x0001;
        const uint VK_Q = 0x51;
        const int WM_HOTKEY = 0x0312;

        const int KindNone = 0;
        const int KindOk = 1;
        const int KindErr = 2;

        readonly System.Windows.Forms.Timer _flash = new System.Windows.Forms.Timer();
        readonly System.Windows.Forms.Timer _hintTimer = new System.Windows.Forms.Timer();
        readonly System.Windows.Forms.Timer _watch = new System.Windows.Forms.Timer();
        static string _ownExe = "QuoteStack";
        IntPtr _lastForeign;
        readonly Font _fCount = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Bold);
        readonly Font _fSub = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular);
        readonly Font _fPlus = new Font("Microsoft YaHei UI", 17f, FontStyle.Bold);
        readonly System.Windows.Forms.Timer _anim = new System.Windows.Forms.Timer();
        readonly NotifyIcon _tray = new NotifyIcon();

        int _plusAt = -1;
        // The combo this instance actually holds, kept so a refused restart can put it back.
        uint _regMods;
        uint _regVk;
        // The text a user must press, in the same shape as config.json. The drawer hints read it
        // instead of hard-coding a combo that stops being true the moment the hotkey is changed.
        public static string HotkeyLabel = "Alt+Shift+Q";

        int _count;
        string _preview;
        string _status;
        string _hint;
        string _cfgPath;
        int _flashKind;
        bool _busy;
        bool _dropPending;   // a dropped picture is being fetched on a worker thread
        Drawer _drawer;
        Point _mouseDown;
        bool _mouseMoved;

        public FloatBar()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(260, 52);
            BackColor = Theme.BarBg;
            DoubleBuffered = true;
            Text = "QuoteStack";
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

            _flash.Interval = 2000;
            _flash.Tick += delegate
            {
                _flash.Stop();
                _flashKind = KindNone;
                _status = null;
                Invalidate();
            };

            // Drives the short "+1" pop; 16ms ticks keep the motion smooth without a
            // second thread or any animation library.
            _anim.Interval = 16;
            _anim.Tick += delegate { Invalidate(); };

            _hintTimer.Interval = 3000;
            _hintTimer.Tick += delegate
            {
                _hintTimer.Stop();
                _hint = null;
                Invalidate();
            };

            // The insert target cannot be decided when the drawer opens: the drawer stays up
            // while the user clicks around, so whatever was foreground at that moment (often
            // Explorer, because that is where SETKEY/BUILD were double-clicked) would be used
            // for a paste meant for another program. The last foreign foreground window is
            // tracked instead, and read at the moment Enter is pressed.
            _ownExe = Process.GetCurrentProcess().ProcessName;
            _watch.Interval = 200;
            _watch.Tick += delegate
            {
                IntPtr fg = Native.GetForegroundWindow();
                if (fg == IntPtr.Zero || fg == Handle) return;
                if (IsOwnWindow(fg)) return;
                _lastForeign = fg;
            };
            _watch.Start();
        }

        static bool IsOwnWindow(IntPtr hwnd)
        {
            string p = Native.ProcessNameOf(hwnd);
            if (p.Length == 0) return true;
            return string.Equals(Path.GetFileNameWithoutExtension(p), _ownExe,
                StringComparison.OrdinalIgnoreCase);
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                return cp;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            PlaceBottomRight();

            _count = Math.Max(0, Store.Count());
            _preview = Store.LastText();

            _tray.Icon = SystemIcons.Application;
            _tray.Text = "QuoteStack";
            var menu = new ContextMenuStrip();
            menu.Items.Add("打开引用列表", null, delegate { OpenDrawer(); });
            menu.Items.Add("设置…", null, delegate { OpenSettings(); });
            menu.Items.Add("清理已引用的内容…", null, delegate { OpenCleanup(); });
            menu.Items.Add("打开配置", null, delegate
            {
                try { Process.Start(new ProcessStartInfo(_cfgPath) { UseShellExecute = true }); }
                catch { }
            });
            menu.Items.Add("退出", null, delegate { Close(); });
            _tray.ContextMenuStrip = menu;
            _tray.Visible = true;

            Invalidate();

            _cfgPath = Config.Ensure();
            string wanted = Config.ReadHotkey();

            uint mods;
            uint vk;
            string parseError;
            if (!Hotkey.Parse(wanted, out mods, out vk, out parseError))
            {
                Store.LogStartup("config-invalid: hotkey=[" + (wanted == null ? "" : wanted) + "] reason=" + parseError);
                MessageBox.Show(
                    "配置文件里的 hotkey 无法使用：" + (wanted == null ? "(空)" : wanted) + "\n原因：" + parseError +
                    "\n\n请修改后重新启动：\n" + _cfgPath,
                    "QuoteStack",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                Environment.Exit(3);
            }

            if (!Native.RegisterHotKey(Handle, HOTKEY_ID, mods, vk))
            {
                int refused = Marshal.GetLastWin32Error();
                Store.LogStartup(HotkeyFailureLine(wanted) + " err=" + refused);
                // A refused combo used to mean "dialog, then exit" — which left nothing running
                // at all (the user then re-launched and hit the same wall). Instead walk a short
                // list of combos that are usually free, keep the first that registers, and write
                // it back so the next launch starts on a key that works.
                string fallback = TryFallbackHotkey(wanted);
                if (fallback == null)
                {
                    Store.LogStartup("hotkey-fallback: none of the candidates registered");
                    MessageBox.Show(
                        "热键 " + wanted + " 注册失败（错误码 " + refused + "），备用组合也都被占用。\n\n" +
                        "请打开配置文件改一个没被占用的组合，保存后重新启动：\n" + _cfgPath,
                        "QuoteStack",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    Environment.Exit(2);
                }
                uint fmods;
                uint fvk;
                string ferr;
                Hotkey.Parse(fallback, out fmods, out fvk, out ferr);
                if (!Native.RegisterHotKey(Handle, HOTKEY_ID, fmods, fvk))
                {
                    // Losing the race between the probe and the real registration is remote, but
                    // it would still leave nothing running, so it is reported rather than ignored.
                    Store.LogStartup(HotkeyFailureLine(fallback) + " err=" + Marshal.GetLastWin32Error());
                    MessageBox.Show("热键 " + fallback + " 在写入配置后被别的程序抢走了，请重新启动。",
                        "QuoteStack", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Environment.Exit(2);
                }
                wanted = fallback;
                mods = fmods;
                vk = fvk;
                _hint = "原热键被占用，已改用 " + fallback;
            }
            else
            {
                _hint = "热键 " + wanted;
            }

            _regMods = mods;
            _regVk = vk;
            HotkeyLabel = wanted;
            Store.LogStartup("hotkey-registered: " + wanted);
            _hintTimer.Start();
            Invalidate();
        }

        // Ordered candidates for the refused-combo path. Only combos that are rarely bound by
        // other software belong here; a candidate that parses but never registers is wasted time.
        public static readonly string[] FallbackHotkeys = new string[]
        {
            // Alt+D is deliberately absent: in browsers it focuses the address bar, so taking it
            // would steal a key people use constantly.
            "Alt+Shift+Q", "Ctrl+Alt+Q", "Ctrl+Shift+Q", "Ctrl+Alt+1"
        };

        // Registers each candidate in turn, unregisters it again and returns the first one that
        // worked. The caller registers it for real, so this is a probe, not the registration.
        string TryFallbackHotkey(string refused)
        {
            for (int i = 0; i < FallbackHotkeys.Length; i++)
            {
                string cand = FallbackHotkeys[i];
                if (string.Equals(cand, refused, StringComparison.OrdinalIgnoreCase)) continue;
                uint m;
                uint k;
                string err;
                if (!Hotkey.Parse(cand, out m, out k, out err)) continue;
                if (!Native.RegisterHotKey(Handle, HOTKEY_ID, m, k)) continue;
                Native.UnregisterHotKey(Handle, HOTKEY_ID);
                Store.LogStartup("hotkey-fallback: " + refused + " -> " + cand);
                try
                {
                    Config.SaveHotkey(cand);
                    Store.LogStartup("hotkey-fallback-written: " + cand);
                }
                catch (Exception ex)
                {
                    // Not fatal for this session; the key still works until the next launch.
                    Store.LogStartup("hotkey-fallback-write-failed: " + ex.GetType().Name);
                }
                return cand;
            }
            return null;
        }

        void PlaceBottomRight()
        {
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(wa.Right - Width - 12, wa.Bottom - Height - 12);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x001A)   // WM_SETTINGCHANGE：系统深浅色被切换
            {
                bool light = Theme.SystemPrefersLight();
                if (light != Theme.IsLight)
                {
                    Theme.Apply(light);
                    BackColor = Theme.BarBg;
                    if (_drawer != null && !_drawer.IsDisposed)
                    {
                        _drawer.BackColor = Theme.PanelBg;
                        _drawer.ListView.BackColor = Theme.DeepBg;
                        _drawer.Invalidate(true);
                        _drawer.ListView.Invalidate();
                    }
                    Invalidate();
                }
            }

            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                // One key, decided by what the user is doing (rules m01388, m03988, m04191, m04356):
                //   drawer focused -> insert (the caret arguments already made)
                //   anywhere else  -> probe for a selection first; a selection is a quote, and
                //                     only "nothing selected" turns the same key into insert.
                // The probe is what makes non-browser programs work: it decides by evidence
                // rather than by knowing which app is in front. Terminals are still skipped
                // because Ctrl+C there is an interrupt, so they go straight to insert.
                bool drawerUp = _drawer != null && !_drawer.IsDisposed && _drawer.Visible;
                if (drawerUp && _drawer.ContainsFocus) { PerformInsert(); return; }
                if (!IsTerminalForeground()) { StartCapture(); return; }
                if (drawerUp) { PerformInsert(); return; }
                OpenOrFocusDrawer();
                return;
            }
            base.WndProc(ref m);
        }

        // Programs where a synthetic Ctrl+C is "interrupt", not "copy". Sending the chord
        // there would kill a running command, so those are refused and the drawer opens.
        // The list errs towards refusing: a wrong entry only costs a convenience, a missing
        // one can abort a build or a long command.
        static readonly string[] TerminalProcs = new string[]
        {
            "cmd.exe", "powershell.exe", "pwsh.exe", "conhost.exe", "wt.exe",
            "WindowsTerminal.exe", "mintty.exe", "bash.exe", "wsl.exe", "ubuntu.exe",
            "Code.exe"
        };

        public static bool IsTerminal(string proc)
        {
            if (proc == null) return false;
            for (int i = 0; i < TerminalProcs.Length; i++)
                if (string.Equals(proc, TerminalProcs[i], StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        static bool IsTerminalForeground()
        {
            return IsTerminal(Native.ProcessNameOf(Native.GetForegroundWindow()));
        }

        public static bool IsEdgeProc(string proc)
        {
            return string.Equals(proc, "msedge.exe", StringComparison.OrdinalIgnoreCase);
        }

        static bool IsEdgeForeground()
        {
            return IsEdgeProc(Native.ProcessNameOf(Native.GetForegroundWindow()));
        }

        // Launch refusals are logged so a failed start is diagnosable, but the log deliberately
        // carries no filesystem path (red line 8: no paths in logs). One source of truth, so a
        // future call site cannot reintroduce the profile path by accident.
        public static string HotkeyFailureLine(string hotkey)
        {
            return "hotkey-register-failed: " + hotkey;
        }

        void StartCapture()
        {
            if (_busy) return;
            _busy = true;
            // Immediate feedback: the capture path can wait up to ~1.6s for modifiers and
            // two chord attempts, and a bar that shows nothing meanwhile looks dead.
            _status = "读取中…";
            _flashKind = KindNone;
            _flash.Stop();
            Invalidate();
            ThreadPool.QueueUserWorkItem(delegate
            {
                CaptureResult r;
                try { r = QuoteStack.Capture.Run(); }
                catch (Exception ex) { r = new CaptureResult { Ok = false, Message = "异常: " + ex.GetType().Name }; }
                try { BeginInvoke((Action)delegate { Apply(r); }); }
                catch { _busy = false; }
            });
        }

        // A drop becomes a quote through the very same storage call as a capture, so a dragged
        // quote behaves identically downstream (list, insert, cleanup). One record per picture;
        // text that came with the drop rides on the first picture instead of being lost.
        //
        // The one outbound request this app makes (fetching the picture a text+image drag points
        // at) runs on a worker thread: DNS and HTTP must never block the UI thread, otherwise an
        // offline machine would freeze the app for seconds in the middle of a drop.
        void SaveDropped(DragIn.Payload p)
        {
            if (p == null) { ShowDropFailure("无法读取拖入内容"); return; }
            // One fetch at a time; a second drop while the first picture is still downloading is
            // ignored rather than queued, so the amount of work stays bounded and debuggable.
            if (_dropPending) { Store.LogStartup("dragin: busy-drop"); _status = "正在取上一张图…"; Invalidate(); return; }
            string text = p.Text == null ? "" : p.Text.Trim();
            string url = p.Url == null ? "" : p.Url;
            // Dragging a picture out of a browser puts the picture's own address into the text
            // format. That is the source of the quote, not its body, so it moves to url and the
            // row shows （图片） with the address under the source line like every other record.
            if (p.Images.Count > 0 && text.Length > 0 && url.Length == 0 && QuoteStack.Capture.LooksLikeUrl(text))
            {
                url = text;
                text = "";
            }
            if (p.Images.Count == 0 && text.Length == 0) { ShowDropFailure("拖入内容为空"); return; }
            // The source line shows the program the drop came from, never a placeholder; the
            // address (when there is one) gets its own line, so nothing repeats. It is taken right
            // now, while the drop is still happening, because fetching shifts the foreground.
            string source = DropSourceLabel();
            // A page drag carries markup but no pixels. The one picture it points at is fetched
            // (the only outbound request this app ever makes); inline data: pictures are decoded
            // locally and need no network at all.
            if (p.Images.Count == 0 && text.Length > 0 && p.Html != null)
            {
                byte[] inline = DragIn.FirstInlineImage(p.Html);
                if (inline != null)
                {
                    p.Images.Add(inline);
                    AppendPayload(p, source, "inline", text, url);
                    return;
                }
                string src = DragIn.FirstImageUrl(p.Html, url);
                if (src == null)
                {
                    AppendPayload(p, source, "none", text, url);
                    return;
                }
                DragIn.Payload payload = p;
                string textCopy = text;
                string urlCopy = url;
                _dropPending = true;
                _status = "取图中…";
                Invalidate();
                ThreadPool.QueueUserWorkItem(delegate
                {
                    string why = "neterr";
                    byte[] png = null;
                    Stopwatch fw = Stopwatch.StartNew();
                    try { png = DragIn.FetchImage(src, 4000, DragIn.MaxImageBytes, out why); }
                    catch (Exception ex) { why = "exception:" + ex.GetType().Name; }
                    if (png != null) { payload.Images.Add(png); why = "ok"; }
                    Store.LogStartup("dragin: fetch=" + why + " imgLen=" + (png == null ? -1 : png.Length)
                        + " costMs=" + fw.ElapsedMilliseconds);
                    string verdict = why;
                    try
                    {
                        BeginInvoke((Action)delegate
                        {
                            _dropPending = false;
                            AppendPayload(payload, source, verdict, textCopy, urlCopy);
                        });
                    }
                    catch
                    {
                        // The drawer went away while the picture was downloading. Nothing has been
                        // written yet, so the drop is dropped whole instead of half-stored.
                        Store.LogStartup("dragin: dropped-after-fetch reason=window-gone");
                    }
                });
                return;
            }
            AppendPayload(p, source, "-", text, url);
        }

        // Everything that touches storage and the floating bar, always on the UI thread. `fetch`
        // is the outcome of the picture fetch ("-" not needed, "inline", "none", "ok", or a
        // failure code); it only feeds the log line and the user-visible hint.
        void AppendPayload(DragIn.Payload p, string source, string fetch, string text, string url)
        {
            int saved = 0;
            string failure = null;
            if (p.Images.Count == 0)
            {
                string err;
                if (Store.Append(text, source, source, url, null, out err)) saved = 1;
                else failure = err;
            }
            else
            {
                for (int i = 0; i < p.Images.Count; i++)
                {
                    string name = Guid.NewGuid().ToString("N").Substring(0, 12) + ".png";
                    string imgErr;
                    if (!Store.SaveImagePng(p.Images[i], name, out imgErr))
                    {
                        if (failure == null) failure = imgErr;
                        continue;
                    }
                    string body = i == 0 ? text : "";
                    string err;
                    if (Store.Append(body, source, source, url, name, out err)) saved++;
                    else
                    {
                        // The row never landed, so the PNG just written would be unreachable:
                        // delete it rather than leave an orphan in images\.
                        if (!Store.DeleteImage(name)) Store.LogStartup("image: orphan-kept " + name);
                        if (failure == null) failure = err;
                    }
                }
            }
            Store.LogStartup("dragin: fmts=" + p.Formats + " fetch=" + fetch + " images=" + p.Images.Count
                + " textLen=" + text.Length + " url=" + (url.Length > 0 ? "ok" : "none")
                + " urlMoved=" + (p.Text != null && p.Text.Trim().Length > 0 && text.Length == 0 ? "yes" : "no")
                + " saved=" + saved);
            if (saved == 0)
            {
                ShowDropFailure(failure == null ? "没有可保存的内容" : failure);
                return;
            }
            _count = Store.Count();
            if (_count < 0) _count = 0;
            if (_drawer != null && !_drawer.IsDisposed && _drawer.Visible)
                _drawer.ListView.ReloadPreserving();
            _preview = text.Length > 0 ? text : "（图片）";
            // A failed picture fetch is not a failed drop: the text is stored, so say exactly
            // that instead of staying silent, and name the reason so it is actionable.
            string fetchNote = FetchReasonText(fetch);
            _status = failure != null ? "部分失败：" + failure
                : (fetchNote != null ? "图片未取到（已存文字）· " + fetchNote : null);
            _flashKind = failure == null && fetchNote == null ? KindOk : KindErr;
            _flash.Interval = 2000;
            _plusAt = Environment.TickCount;
            _anim.Start();
            _flash.Stop();
            _flash.Start();
            Invalidate();
        }

        // Turns a fetch outcome into something a person can act on; null means "nothing to say"
        // (the fetch was not attempted, unnecessary, or successful).
        internal static string FetchReasonText(string fetch)
        {
            if (fetch == null) return null;
            switch (fetch)
            {
                case "-":
                case "ok":
                case "inline":
                case "none":
                    return null;
                case "blocked": return "图片地址被拒绝";
                case "badurl": return "图片地址无效";
                case "timeout": return "取图超时";
                case "toobig": return "图片超过 20MB";
                case "notimage": return "取回的不是图片";
                case "tiny": return "图片太小（追踪像素）";
                case "neterr": return "网络不可用";
                case "http-403": return "图床拒绝（403）";
                case "http-404": return "图床找不到（404）";
            }
            if (fetch.StartsWith("http-", StringComparison.Ordinal))
                return "图床返回 " + fetch.Substring(5);
            if (fetch.StartsWith("exception:", StringComparison.Ordinal))
                return "取图出错（" + fetch.Substring(10) + "）";
            return "图片未取到";
        }

        // A drop carries no process name, so the source is taken from the last foreign foreground
        // window (the app the user was in just before dragging). Falls back to「拖入」only when
        // even that is unknown, so no record is ever labelled with a placeholder.
        string DropSourceLabel()
        {
            IntPtr fg = Native.GetForegroundWindow();
            string proc = (fg != IntPtr.Zero && !IsOwnWindow(fg)) ? Native.ProcessNameOf(fg) : null;
            if (proc == null || proc.Length == 0)
            {
                if (_lastForeign != IntPtr.Zero) proc = Native.ProcessNameOf(_lastForeign);
            }
            string label = Records.AppLabel(proc);
            return (label == null || label.Length == 0 || label == "?") ? "拖入" : label;
        }

        void ShowDropFailure(string why)
        {
            _status = "拖入失败：" + why;
            _flashKind = KindErr;
            _flash.Interval = 2000;
            _flash.Stop();
            _flash.Start();
            Invalidate();
        }

        void Apply(CaptureResult r)
        {
            _busy = false;
            if (r.Ok)
            {
                _count = Store.Count();
                if (_count < 0) _count = 0;
                // The drawer can be open while the user captures again. Refresh it in place so
                // the new quote appears without closing the drawer; ticks, expanded rows and
                // scroll position are carried over (see QuoteListView.ReloadPreserving).
                if (_drawer != null && !_drawer.IsDisposed && _drawer.Visible)
                    _drawer.ListView.ReloadPreserving();
                _preview = r.Preview;
                _status = null;
                _flashKind = KindOk;
                _flash.Interval = 2000;
                _plusAt = Environment.TickCount;
                _anim.Start();
            }
            else if (!r.HadSelection)
            {
                // Nothing was selected where the key was pressed, so the same key means "insert".
                // If the drawer is already open that is the whole action — asking the user to
                // press again because the drawer happened to be open was the "按好几下才插入"
                // complaint (m04191).
                _status = null;
                _flashKind = KindNone;
                _flash.Stop();
                Invalidate();
                if (_drawer != null && !_drawer.IsDisposed && _drawer.Visible)
                {
                    PerformInsert();
                    return;
                }
                OpenOrFocusDrawer();
                return;
            }
            else
            {
                _status = r.Message;
                _flashKind = KindErr;
                _flash.Interval = 1500;
            }
            _flash.Stop();
            _flash.Start();
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            // The bar is WS_EX_NOACTIVATE, so the click cannot be turned into a caption
            // drag directly; the position is tracked and the window moved by hand.
            _mouseDown = e.Location;
            _mouseMoved = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if ((e.Button & MouseButtons.Left) == 0) return;
            if (!_mouseMoved
                && Math.Abs(e.X - _mouseDown.X) < 4
                && Math.Abs(e.Y - _mouseDown.Y) < 4) return;
            _mouseMoved = true;
            Location = new Point(Location.X + (e.X - _mouseDown.X), Location.Y + (e.Y - _mouseDown.Y));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;
            if (!_mouseMoved) OpenDrawer();
        }

        // Used by the hotkey path: an already open drawer is brought forward instead of
        // being closed, because the key there means "insert here", not "toggle".
        void OpenOrFocusDrawer()
        {
            if (_drawer != null && !_drawer.IsDisposed && _drawer.Visible)
            {
                _drawer.BringToFront();
                _drawer.Activate();
                _drawer.ListView.Focus();
                return;
            }
            OpenDrawer();
        }

        void OpenDrawer()
        {
            if (_drawer != null && !_drawer.IsDisposed)
            {
                _drawer.Close();
                return;
            }
            // Remembered before the drawer appears: this is where the quotes go back to.
            IntPtr target = Native.GetForegroundWindow();
            _drawer = new Drawer(target);
            _drawer.FormClosed += delegate { _drawer = null; Invalidate(); };
            _drawer.CleanupRequested += delegate { OpenCleanup(); };
            _drawer.DeleteRequested += delegate { DeleteTicked(); };
            _drawer.SettingsRequested += delegate { OpenSettings(); };
            _drawer.Dropped += delegate(DragIn.Payload p) { SaveDropped(p); };
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            // Opens at the size the user last left it at (clamped to this screen); the floor is
            // Drawer.MinW/MinH, the narrowest width that still fits the three bar entries.
            int w = Math.Min(460, wa.Width - 48);
            int h = Math.Min(520, wa.Height - 96);
            int rememberedW, rememberedH;
            if (Config.ReadDrawerSize(out rememberedW, out rememberedH))
            {
                w = rememberedW;
                h = rememberedH;
            }
            if (w > wa.Width - 48) w = wa.Width - 48;
            if (h > wa.Height - 64) h = wa.Height - 64;
            if (w < Drawer.MinW) w = Math.Min(Drawer.MinW, wa.Width - 48);
            if (h < Drawer.MinH) h = Math.Min(Drawer.MinH, wa.Height - 64);
            _drawer.Size = new Size(w, h);
            _drawer.Location = new Point(wa.Right - 16 - w, wa.Top + (wa.Height - h) / 2);
            _drawer.Show();
            _drawer.BringToFront();
            _drawer.Activate();
            _drawer.ListView.Focus();
        }

        // Batch delete of the quotes that were already inserted at least once. The
        // rewrite itself is atomic (Store.DeleteByKey); the user confirms the count
        // first because there is no undo.
        void OpenCleanup()
        {
            List<Record> used = Records.LoadUsedOnly();
            bool hadDrawer = _drawer != null && !_drawer.IsDisposed && _drawer.Visible;
            if (hadDrawer) _drawer.Hide();
            try
            {
                using (CleanupForm f = new CleanupForm(used))
                {
                    f.ShowDialog(this);
                    if (f.Deleted > 0)
                    {
                        _count = Math.Max(0, Store.Count());
                        _status = "已删除 " + f.Deleted + " 条已引用内容";
                        _flashKind = KindOk;
                        _flash.Interval = 3000; // a deletion message is worth reading
                        _flash.Stop();
                        _flash.Start();
                        Invalidate();
                    }
                }
            }
            finally
            {
                if (hadDrawer && _drawer != null && !_drawer.IsDisposed)
                {
                    _drawer.Show();
                    _drawer.BringToFront();
                    _drawer.Activate();
                    _drawer.ListView.Reload();
                    _drawer.ListView.Focus();
                }
            }
        }

        // Opens the hotkey editor. "Save" only writes the file; "Save and restart" hands over to
        // RestartApp, which is the only path that can make the new key live in this session.
        void OpenSettings()
        {
            string current = Config.ReadHotkey();
            using (SettingsForm f = new SettingsForm(current))
            {
                f.ShowDialog(this);
                if (f.Saved == null) return;
                if (f.RestartRequested)
                {
                    RestartApp(f.Saved);
                    return;
                }
                _status = "热键已保存 · 重启后生效";
                _flashKind = KindOk;
                _flash.Interval = 3000;
                _flash.Stop();
                _flash.Start();
                Invalidate();
            }
        }

        // Replaces the running instance with one that reads the newly saved hotkey. The combo is
        // probed first: a replacement that cannot register ends up sitting in a modal dialog
        // looking alive, and the old instance would then exit on that false signal (seen 19:22 —
        // Alt+Q was already taken by another program, both instances ended up gone).
        void RestartApp(string hotkey)
        {
            uint mods;
            uint vk;
            string error;
            if (!Hotkey.Parse(hotkey, out mods, out vk, out error))
            {
                MessageBox.Show(this, "热键格式不对：" + error, "QuoteStack",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 1) Can this combo be registered at all? Ask before disturbing the running instance.
            Native.UnregisterHotKey(Handle, HOTKEY_ID);
            if (!Native.RegisterHotKey(Handle, HOTKEY_ID, mods, vk))
            {
                int refused = Marshal.GetLastWin32Error();
                Native.RegisterHotKey(Handle, HOTKEY_ID, _regMods, _regVk);
                Store.LogStartup("restart-abort: " + hotkey + " taken err=" + refused);
                MessageBox.Show(this,
                    "热键 " + hotkey + " 无法注册（错误码 " + refused + "），可能已被其他程序占用。\n原热键保持不变。",
                    "QuoteStack", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Native.UnregisterHotKey(Handle, HOTKEY_ID);
            Store.LogStartup("restart: probe-ok " + hotkey);

            // 2) Start the replacement and wait for it to *report* a successful registration.
            long watermark = Store.LogLength();
            Process started = null;
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(Application.ExecutablePath);
                psi.UseShellExecute = false;
                psi.WorkingDirectory = Path.GetDirectoryName(Application.ExecutablePath);
                started = Process.Start(psi);
            }
            catch (Exception ex)
            {
                Store.LogStartup("restart-failed: " + ex.GetType().Name);
                Native.RegisterHotKey(Handle, HOTKEY_ID, _regMods, _regVk);
                MessageBox.Show(this, "重启失败：新实例没能启动。原热键保持不变。",
                    "QuoteStack", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string verdict = WaitRestartVerdict(watermark, hotkey, started, 6000);
            if (verdict == "ok")
            {
                Store.LogStartup("restart: " + hotkey);
                Close();
                return;
            }

            // 3) The replacement did not come up. Take it down (it may be stuck on a dialog) and
            // keep serving on the old combo, rather than leaving the user with nothing running.
            if (started != null && !started.HasExited)
            {
                try { started.Kill(); } catch { }
            }
            Native.RegisterHotKey(Handle, HOTKEY_ID, _regMods, _regVk);
            Store.LogStartup("restart-failed: " + verdict + " " + hotkey);
            MessageBox.Show(this,
                "新热键 " + hotkey + " 没能启用（" +
                (verdict == "reported-failure" ? "新实例报告注册失败" : "等不到新实例注册成功") +
                "）。\n原热键已恢复。",
                "QuoteStack", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // Waits for the replacement's own log line. Process liveness proves nothing here: an
        // instance that failed to register sits in a MessageBox and looks perfectly healthy.
        string WaitRestartVerdict(long watermark, string hotkey, Process started, int timeoutMs)
        {
            string ok = "hotkey-registered: " + hotkey;
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                if (Store.LogHasAfter(watermark, ok, 20000)) return "ok";
                if (Store.LogHasAfter(watermark, "hotkey-register-failed", 20000)) return "reported-failure";
                if (Store.LogHasAfter(watermark, "config-invalid", 20000)) return "reported-failure";
                if (started != null && started.HasExited) return "exited";
                Thread.Sleep(150);
            }
            return "timeout";
        }

        // Deletes exactly the ticked quotes, straight from the drawer's bottom bar. Same
        // atomic rewrite as the cleanup panel; the confirmation exists because there is
        // no undo.
        void DeleteTicked()
        {
            if (_drawer == null || _drawer.IsDisposed) return;
            List<Record> picked = _drawer.ListView.Checked();
            if (picked.Count == 0) return;
            DialogResult ask = MessageBox.Show(_drawer,
                "删除勾选的 " + picked.Count + " 条引用？删除后不可恢复。",
                "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (ask != DialogResult.Yes) return;

            HashSet<string> drop = new HashSet<string>(StringComparer.Ordinal);
            foreach (Record r in picked) drop.Add(r.Key);
            int removed;
            string error;
            if (!Store.DeleteByKey(drop, out removed, out error))
            {
                _drawer.ListView.SetHint("删除失败：" + error);
                return;
            }
            _count = Math.Max(0, Store.Count());
            _drawer.ListView.Reload();
            _drawer.ListView.SetHint("已删除 " + removed + " 条");
            _status = "已删除 " + removed + " 条";
            _flashKind = KindOk;
            _flash.Interval = 3000; // same reading time as the cleanup panel's message
            _flash.Stop();
            _flash.Start();
            Invalidate();
        }

        void PerformInsert()
        {
            if (_drawer == null || _drawer.IsDisposed) return;
            List<Record> chosen = _drawer.ListView.Checked();
            if (chosen.Count == 0)
            {
                // Nothing ticked: nothing is inserted. The ticks are cleared after an insert
                // on purpose, so a second Enter must not paste the same quote again.
                _drawer.ListView.SetHint("没有勾选内容 · 先勾选再按热键（" + FloatBar.HotkeyLabel + "）");
                return;
            }
            string text = Records.Compose(chosen, DateTimeOffset.Now);
            // Where the caret actually is now, not where it was when the drawer opened.
            // The window the user pressed the key in wins: the 200ms watcher can still be one
            // tick behind right after a click, and a stale target pastes into the wrong app.
            IntPtr fgNow = Native.GetForegroundWindow();
            IntPtr target = (fgNow != IntPtr.Zero && !IsOwnWindow(fgNow)) ? fgNow : _lastForeign;
            if (target == IntPtr.Zero) target = _drawer.TargetWindow;
            // Hidden rather than closed: it has to give the target back its focus for the
            // paste, then returns so the next quote can be inserted in the same pass.
            _drawer.Hide();

            string message;
            string log;
            bool anyImage = false;
            foreach (Record r in chosen) if (r.HasImage) anyImage = true;
            // Text-only selections still paste as one composed block (blank line between
            // quotes); a selection containing pictures has to be pasted record by record.
            bool ok = anyImage
                ? Deliver.RunItems(chosen, target, out message, out log)
                : Deliver.Run(target, text, out message, out log);
            Store.LogStartup(log); // counts and outcomes only, never the text

            if (ok)
            {
                // Recorded per quote, in a separate journal, so the cleanup panel can
                // offer exactly the quotes that were already used.
                int marked = 0;
                foreach (Record r in chosen)
                {
                    if (Store.AppendUsed(r.Key)) marked++;
                    r.Used = true;
                }
                Store.LogStartup("deliver: marked-used=" + marked + "/" + chosen.Count);
                _count = Math.Max(0, Store.Count());
                _preview = chosen[0].DisplayText;
                _status = "已插入 " + chosen.Count + " 条";
                _flashKind = KindOk;
                // Reload re-reads the stack, which both clears the ticks (a second insert
                // must not repeat the same quote) and paints the "已引用" marker.
                _drawer.ListView.Reload();
                _drawer.ListView.SetHint("已插入 " + chosen.Count + " 条 · 可继续勾选");
            }
            else
            {
                _status = message;
                _flashKind = KindErr;
                _drawer.ListView.SetHint(message == null ? "插入失败" : message);
            }
            _flash.Stop();
            _flash.Start();
            Invalidate();

            if (_drawer != null && !_drawer.IsDisposed)
            {
                _drawer.Show();
                _drawer.BringToFront();
                _drawer.Activate();
                _drawer.ListView.Focus();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Native.UnregisterHotKey(Handle, HOTKEY_ID);
            _tray.Visible = false;
            _tray.Dispose();
            base.OnFormClosing(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color bg = Theme.BarBg;
            if (_flashKind == KindOk) bg = Theme.FlashOk;
            else if (_flashKind == KindErr) bg = Theme.FlashErr;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Rounded(rect, 12))
            {
                using (var b = new SolidBrush(bg)) g.FillPath(b, path);
                Theme.DrawRim(g, path, rect);
            }

            string cnt = _count.ToString() + " 条";
            SizeF cntSize = g.MeasureString(cnt, _fCount);
            using (var b = new SolidBrush(Color.FromArgb(245, 245, 247)))
                g.DrawString(cnt, _fCount, b, 14f, (Height - cntSize.Height) / 2f);

            float x = 14f + cntSize.Width + 10f;
            float avail = Width - x - 12f;

            // "+1" 画在 (Width - ps.Width - 12)，与正文可用区右端重合，不先让位就会压在正文尾巴上。
            bool plusUp = _plusAt >= 0;
            SizeF ps = plusUp ? g.MeasureString("+1", _fPlus) : SizeF.Empty;
            if (plusUp) avail -= ps.Width + 10f;

            string sub;
            Color subColor;
            if (_status != null)
            {
                sub = _status;
                subColor = Theme.Warn;
            }
            else if (_hint != null)
            {
                sub = _hint;
                subColor = Theme.HintText;
            }
            else
            {
                sub = _preview == null ? "还没有采集" : Flatten(_preview);
                subColor = Theme.PreviewText;
            }
            if (avail > 8f)
            {

            while (sub.Length > 0 && g.MeasureString(sub, _fSub).Width > avail)
                sub = sub.Substring(0, sub.Length - 1);

            if (sub.Length > 0)
            {
                SizeF subSize = g.MeasureString(sub, _fSub);
                using (var b = new SolidBrush(subColor))
                    g.DrawString(sub, _fSub, b, x, (Height - subSize.Height) / 2f);
            }
            }

            // "+1" feedback (user rule m01675): the count changing is too quiet to notice,
            // so the bar pops a bright +1 that rises ~10px and fades out over 900ms.
            if (_plusAt >= 0)
            {
                int el = Environment.TickCount - _plusAt;
                if (el < 0 || el > 900)
                {
                    _plusAt = -1;
                    _anim.Stop();
                }
                else
                {
                    float t = el / 900f;
                    int alpha = (int)(255f * (1f - t * t));
                    float dy = -6f * t;   // 17pt 字高 31.6px，52px 条里升 10px 会贴边
                    if (alpha > 0)
                    {
                        using (var b = new SolidBrush(Color.FromArgb(alpha, Theme.OrdinalText)))
                            g.DrawString("+1", _fPlus, b, Width - ps.Width - 12f, (Height - ps.Height) / 2f + dy);
                    }
                }
            }
        }

        static string Flatten(string s)
        {
            var sb = new StringBuilder(s.Length);
            bool space = false;
            foreach (char c in s)
            {
                if (char.IsWhiteSpace(c)) { space = true; continue; }
                if (space && sb.Length > 0) sb.Append(' ');
                space = false;
                sb.Append(c);
            }
            return sb.ToString();
        }

        static GraphicsPath Rounded(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    class Record
    {
        public string Text;
        public string Source;
        public string Url;
        // File name inside %APPDATA%\quote-stack\images. Null for plain text quotes; a
        // record may hold text AND an image (a mixed selection copied from a page).
        public string Image;
        public DateTimeOffset When;
        // Per-record identity written at capture time. v1 rows have none; for those the
        // identity falls back to "h:" + the text hash, which is how v1 deleted them.
        public string Id;
        // True once this quote has been inserted at least once; drives the marker in
        // the list and the cleanup panel.
        public bool Used;

        public bool HasImage
        {
            get { return Image != null && Image.Length > 0; }
        }

        // What the list and the floating bar show for this record.
        public string DisplayText
        {
            get
            {
                if (Text != null && Text.Trim().Length > 0) return Text;
                return "（图片）";
            }
        }

        public string Key
        {
            get
            {
                if (Id != null && Id.Length > 0) return Id;
                return "h:" + Store.Hash12(Text == null ? "" : Text);
            }
        }
    }

    static class Records
    {
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        // Newest first: the drawer is read top-down and the fresh quote is what the
        // user is usually after.
        public static List<Record> Load(int max)
        {
            List<Record> list = new List<Record>();
            try
            {
                string path = Store.FilePath;
                if (!File.Exists(path)) return list;
                HashSet<string> used = Store.LoadUsed();
                List<string> lines = new List<string>();
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader sr = new StreamReader(fs, Utf8))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                        if (line.Trim().Length > 0) lines.Add(line);
                }
                for (int i = lines.Count - 1; i >= 0 && list.Count < max; i--)
                {
                    string text = Store.JsonField(lines[i], "text");
                    string image = Store.JsonField(lines[i], "image");
                    bool hasImage = image != null && image.Length > 0;
                    if ((text == null || text.Trim().Length == 0) && !hasImage) continue;
                    Record r = new Record();
                    r.Id = Store.JsonField(lines[i], "id");
                    r.Text = text;
                    r.Source = AppLabel(Store.JsonField(lines[i], "source"));
                    r.Url = Store.JsonField(lines[i], "url");
                    r.Image = hasImage ? image : null;
                    r.When = ParseWhen(Store.JsonField(lines[i], "ts"));
                    // New journals key on the record id, v1 journals keyed on the text hash;
                    // both are honoured so an upgrade never loses the 已引用 markers.
                    r.Used = used.Contains(r.Key) || used.Contains(Store.Hash12(text));
                    list.Add(r);
                }
            }
            catch (Exception ex)
            {
                // A damaged line must not look like an empty stack. Only the exception type
                // is logged: never the text that failed to parse.
                Store.LogStartup("load: " + ex.GetType().Name + " (part of the list may be missing)");
            }
            return list;
        }

        // Only the quotes that were already inserted at least once, newest first.
        public static List<Record> LoadUsedOnly()
        {
            List<Record> all = Load(int.MaxValue);
            List<Record> only = new List<Record>();
            foreach (Record r in all) if (r.Used) only.Add(r);
            return only;
        }

        public static string AppLabel(string proc)
        {
            if (proc == null || proc.Length == 0) return "?";
            if (FloatBar.IsEdgeProc(proc)) return "Edge";
            if (string.Equals(proc, "WINWORD.EXE", StringComparison.OrdinalIgnoreCase)) return "Word";
            if (string.Equals(proc, "wps.exe", StringComparison.OrdinalIgnoreCase)) return "WPS";
            if (string.Equals(proc, "Weixin.exe", StringComparison.OrdinalIgnoreCase)) return "微信";
            if (string.Equals(proc, "WeChat.exe", StringComparison.OrdinalIgnoreCase)) return "微信";
            if (string.Equals(proc, "notepad.exe", StringComparison.OrdinalIgnoreCase)) return "记事本";
            if (string.Equals(proc, "Acrobat.exe", StringComparison.OrdinalIgnoreCase)) return "Acrobat";
            if (string.Equals(proc, "SumatraPDF.exe", StringComparison.OrdinalIgnoreCase)) return "SumatraPDF";
            if (proc.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return proc.Substring(0, proc.Length - 4);
            return proc;
        }

        static DateTimeOffset ParseWhen(string ts)
        {
            if (ts != null)
            {
                DateTimeOffset dto;
                if (DateTimeOffset.TryParse(ts, out dto)) return dto;
            }
            return DateTimeOffset.Now;
        }

        public static string GroupOf(Record r, DateTimeOffset now)
        {
            int days = (now.Date - r.When.LocalDateTime.Date).Days;
            if (days <= 0) return "今日";
            if (days <= 3) return "三日内";
            if (days <= 7) return "一周内";
            return "更早";
        }

        // Year is shown only when it differs from this year, so the common case stays short.
        public static string Stamp(Record r, DateTimeOffset now)
        {
            DateTime t = r.When.LocalDateTime;
            return t.Year == now.Year ? t.ToString("MM-dd HH:mm") : t.ToString("yyyy-MM-dd HH:mm");
        }

        // Where the quote came from: the address bar URL when one was captured, otherwise
        // just the application name. The page title is stored but never printed.
        public static string SrcLabel(Record r)
        {
            if (r.Url != null && r.Url.Length > 0) return r.Url;
            return r.Source;
        }

        // Body only: the source line and the capture time stay in the drawer and in the
        // record, but they are not written into the user's document (user rule m01388).
        public static string Compose(List<Record> chosen, DateTimeOffset now)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < chosen.Count; i++)
            {
                if (i > 0) sb.Append("\n\n");
                sb.Append(chosen[i].Text.Trim());
            }
            return sb.ToString();
        }
    }

    static class Deliver
    {
        // Puts the composed text on the clipboard, pastes it into the window that was
        // foreground before the drawer opened, then restores the previous clipboard.
        // Nothing is typed unless that window is verified as foreground (T3 rule).
        public static bool Run(IntPtr target, string text, out string message, out string log)
        {
            message = null;
            Stopwatch sw = Stopwatch.StartNew();

            Native.ClipBackup backup;
            int formats;
            uint seq;
            // Bug fix: the return value used to be ignored. When the clipboard could not
            // be read at all, backup stayed null and the restore below called
            // EmptyClipboard(), which silently wiped whatever the user had copied.
            bool hadSnapshot = Native.ClipboardSnapshot(out backup, out formats, out seq);
            if (!hadSnapshot)
            {
                message = "剪贴板被占用，未发送按键";
                log = "deliver: abort snapshot-failed seq=" + seq + " textLen=" + text.Length;
                return false;
            }
            // Text and images are backed up; anything else (files, rich text) is not, and
            // in that case leaving our content on the clipboard is better than emptying it.
            bool canRestore = (backup != null && (backup.Text != null || backup.Image != null)) || formats == 0;
            if (!Native.SetClipboardText(text))
            {
                message = "剪贴板写入失败";
                log = "deliver: clipboard-set-failed";
                return false;
            }

            // The hotkey fires on key-down, so the user's Alt/Shift can still be held and
            // a V typed now would arrive as Ctrl+Alt+Shift+V, which pastes nothing.
            long waitedMods = Native.WaitModifiersReleased(1200);
            int released = 0;
            if (!Native.HeldModifiers().Equals("-"))
            {
                released = Native.ForceReleaseModifiers();
                Thread.Sleep(40);
            }

            if (!Native.EnsureForeground(target, 1500))
            {
                // The text stays on the clipboard on purpose: the user can still paste it
                // by hand, and nothing was typed into an unverified window.
                message = "已复制，按 Ctrl+V";
                log = "deliver: target-lost target=" + Native.ProcessNameOf(target)
                    + " waitedMods=" + waitedMods + " released=" + released
                    + " textLen=" + text.Length + " restored=false";
                return false;
            }

            // The foreground window is already the target at this point, but Word/WeChat
            // restore their own caret focus a moment later; a V sent in between lands on a
            // window that is active but has no caret yet, which is exactly the "says inserted,
            // nothing appears" symptom. 180ms is a heuristic, not a measurement.
            Thread.Sleep(180);

            int sent = Native.SendCtrlV(25);
            // The target reads the clipboard while handling the key press; swapping it back
            // immediately can paste empty. 400ms is a heuristic, and the cost is logged.
            Thread.Sleep(400);
            // Who actually had the focus when the key went in: without this the log cannot
            // tell "V was swallowed" from "V went to the wrong window".
            string fgAfter = Native.ProcessNameOf(Native.GetForegroundWindow());
            if (canRestore) Native.ClipboardRestore(backup);

            // Bug fix: SendInput returns how many events it actually inserted. A count
            // below 4 means the paste chord never reached any window, and reporting
            // "已插入" for that was a lie the UI told.
            if (sent != 4)
            {
                message = "粘贴失败（按键未送达）";
                log = "deliver: send-failed sent=" + sent + " target=" + Native.ProcessNameOf(target)
                    + " fgAfterSend=" + fgAfter + " restored=" + canRestore
                    + " textLen=" + text.Length + " costMs=" + sw.ElapsedMilliseconds;
                return false;
            }

            log = "deliver: sent=" + sent + " target=" + Native.ProcessNameOf(target)
                + " fgAfterSend=" + fgAfter
                + " waitedMods=" + waitedMods + " released=" + released
                + " textLen=" + text.Length + " restored=" + canRestore
                + " costMs=" + sw.ElapsedMilliseconds;
            return true;
        }

        // Mixed selections: one paste per record (text first, then its picture), because a
        // target application takes either text or an image in a single paste, not both. The
        // clipboard is backed up once and restored once around the whole pass.
        public static bool RunItems(List<Record> items, IntPtr target, out string message, out string log)
        {
            message = null;
            log = null;
            Stopwatch sw = Stopwatch.StartNew();
            Native.ClipBackup backup;
            int formats;
            uint seq;
            if (!Native.ClipboardSnapshot(out backup, out formats, out seq))
            {
                message = "剪贴板被占用，未发送按键";
                log = "deliver-items: abort snapshot-failed seq=" + seq;
                return false;
            }
            bool canRestore = (backup != null && (backup.Text != null || backup.Image != null)) || formats == 0;

            // Same modifier race as the text path: the hotkey fires on key-down.
            long waitedMods = Native.WaitModifiersReleased(1200);
            int released = 0;
            if (!Native.HeldModifiers().Equals("-")) { released = Native.ForceReleaseModifiers(); Thread.Sleep(40); }

            int steps = 0;
            int pasted = 0;
            string failed = null;
            string fgLast = "-";
            foreach (Record r in items)
            {
                if (r.Text != null && r.Text.Trim().Length > 0)
                {
                    steps++;
                    if (!Native.SetClipboardText(r.Text)) { failed = "clipboard-set-failed"; break; }
                    if (!PasteOnce(target, ref fgLast)) { failed = "send-failed"; break; }
                    pasted++;
                }
                if (r.HasImage)
                {
                    byte[] png = null;
                    try
                    {
                        string ip = Store.ImagePath(r.Image);
                        if (ip != null && File.Exists(ip)) png = File.ReadAllBytes(ip);
                    }
                    catch (Exception ex) { Store.LogStartup("image: read-failed " + ex.GetType().Name); }
                    steps++;
                    if (png == null) { failed = "image-missing"; break; }
                    if (!Native.SetClipboardImagePng(png)) { failed = "image-set-failed"; break; }
                    if (!PasteOnce(target, ref fgLast)) { failed = "send-failed"; break; }
                    pasted++;
                }
            }
            if (canRestore) Native.ClipboardRestore(backup);

            string tail = " steps=" + steps + " pasted=" + pasted
                + " target=" + Native.ProcessNameOf(target) + " fgAfterSend=" + fgLast
                + " waitedMods=" + waitedMods + " released=" + released
                + " restored=" + canRestore + " costMs=" + sw.ElapsedMilliseconds;
            if (failed != null)
            {
                message = failed == "image-missing" ? "图片文件缺失" : "粘贴失败（按键未送达）";
                log = "deliver-items: failed=" + failed + tail;
                return false;
            }
            log = "deliver-items: ok" + tail;
            return true;
        }

        // One Ctrl+V into the target. False means SendInput inserted fewer than the four
        // events (the chord never reached a window) or the target never came forward.
        static bool PasteOnce(IntPtr target, ref string fgLast)
        {
            if (!Native.EnsureForeground(target, 1500)) return false;
            Thread.Sleep(180);
            int sent = Native.SendCtrlV(25);
            Thread.Sleep(400);
            fgLast = Native.ProcessNameOf(Native.GetForegroundWindow());
            return sent == 4;
        }
    }

    class QuoteListView : Control
    {
        const int GroupHeight = 30;
        const int PadLeft = 16;
        const int PadRight = 16;
        const int CtrlZone = 56;
        const int BodyLineH = 18;
        const int BodyTop = 10;
        const int MetaH = 16;
        const int UrlH = 18;
        const int BottomBarH = 34;
        const int RowPadBottom = 12;

        readonly Font _fBody = new Font("Microsoft YaHei UI", 10f, FontStyle.Regular);
        readonly Font _fGroup = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
        readonly Font _fMeta = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular);
        readonly Font _fBar = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular);
        readonly Font _fOrd = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold);

        List<Record> _items = new List<Record>();
        bool[] _open = new bool[0];
        bool[] _checked = new bool[0];

        // Tick order, newest action last. Insertion follows this list, not the row order:
        // the user picks quotes in the order they want them to appear in the document
        // (user rule m01726).
        readonly List<Record> _picks = new List<Record>();
        float[] _y = new float[0];
        float[] _h = new float[0];
        float[] _bodyH = new float[0];
        readonly List<string> _groupNames = new List<string>();
        readonly List<float> _groupY = new List<float>();
        float _total;
        int _sel = -1;
        int _scroll;
        string _hint;

        public event EventHandler CloseRequested;

        public event EventHandler CleanupRequested;

        // Raised by the "删除 N 项" control in the bottom bar. The drawer confirms and does
        // the actual deletion, because only the host form can own a modal dialog.
        public event EventHandler DeleteRequested;

        // Hit area of the "清理已引用" label in the bottom bar; empty when hidden.
        RectangleF _clearRect;

        // Hit areas of "删除 N 项" / "全选" in the bottom bar; empty when hidden.
        RectangleF _delRect;
        RectangleF _allRect;

        public QuoteListView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint | ControlStyles.Selectable, true);
            BackColor = Theme.DeepBg;
            TabStop = true;
        }

        public void Reload()
        {
            ClearThumbs();
            _items = Records.Load(int.MaxValue);
            _open = new bool[_items.Count];
            _checked = new bool[_items.Count]; // nothing is checked until the user says so
            _picks.Clear();
            _sel = _items.Count > 0 ? 0 : -1;
            _scroll = 0;
            _hint = null;
            Invalidate();
        }

        public int Count { get { return _items.Count; } }

        // Same as Reload, but for a list the user is looking at: the tick order, which rows
        // are expanded, the focused row and the scroll offset all survive a new capture.
        // Identity is the record key, never the row index, because a new capture shifts every
        // index by one.
        public void ReloadPreserving()
        {
            ClearThumbs();
            List<Record> fresh = Records.Load(int.MaxValue);
            // A read that suddenly returns nothing while rows are on screen is far more likely
            // to be a locked/renamed file than the user deleting everything: keep what is
            // shown instead of blanking the drawer.
            if (fresh.Count == 0 && _items.Count > 0)
            {
                SetHint("刷新失败（文件被占用？）");
                return;
            }

            List<string> pickKeys = new List<string>(_picks.Count);
            foreach (Record r in _picks) pickKeys.Add(r.Key);
            HashSet<string> openKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _items.Count && i < _open.Length; i++)
                if (_open[i]) openKeys.Add(_items[i].Key);
            string selKey = (_sel >= 0 && _sel < _items.Count) ? _items[_sel].Key : null;
            int scroll = _scroll;

            _items = fresh;
            _open = new bool[fresh.Count];
            _checked = new bool[fresh.Count];
            _picks.Clear();
            _sel = fresh.Count > 0 ? 0 : -1;
            _hint = null;

            // Re-tick in the original pick order: Toggle appends to _picks, so the insert
            // order the user built up is reproduced exactly.
            for (int k = 0; k < pickKeys.Count; k++)
            {
                int idx = IndexOfKey(pickKeys[k]);
                if (idx >= 0) Toggle(idx);
            }
            for (int i = 0; i < fresh.Count; i++)
                if (openKeys.Contains(fresh[i].Key)) _open[i] = true;
            if (selKey != null)
            {
                int s = IndexOfKey(selKey);
                if (s >= 0) _sel = s;
            }
            _scroll = scroll;
            Invalidate();
        }

        int IndexOfKey(string key)
        {
            for (int i = 0; i < _items.Count; i++)
                if (string.Equals(_items[i].Key, key, StringComparison.Ordinal)) return i;
            return -1;
        }

        // The only place a tick changes, so the pick order can never drift from the ticks.
        public void Toggle(int i)
        {
            if (i < 0 || i >= _items.Count || _checked.Length != _items.Count) return;
            Record r = _items[i];
            if (_checked[i])
            {
                _checked[i] = false;
                _picks.Remove(r);
            }
            else
            {
                _checked[i] = true;
                if (!_picks.Contains(r)) _picks.Add(r);
            }
            _sel = i;
            _hint = null;
            Invalidate();
        }

        public void SetHint(string hint)
        {
            _hint = hint;
            Invalidate();
        }

        // Ticks everything; when everything is already ticked it clears instead, so the
        // same control is useful both ways ("全选" / "取消全选").
        public void ToggleSelectAll()
        {
            int n = _items.Count;
            if (n == 0 || _checked.Length != n) return;
            int cnt = 0;
            for (int i = 0; i < n; i++) if (_checked[i]) cnt++;
            bool select = cnt < n;
            for (int i = 0; i < n; i++) if (_checked[i] != select) Toggle(i);
            _hint = select ? ("已全选 " + n + " 条") : null;
            Invalidate();
        }

        public List<Record> Checked()
        {
            // Pick order, not row order: the document should read in the sequence the
            // quotes were ticked.
            List<Record> r = new List<Record>(_picks.Count);
            for (int k = 0; k < _picks.Count; k++) r.Add(_picks[k]);
            return r;
        }

        // 1-based position in the pick order, 0 when the record is not ticked. Painted
        // next to the tick so the insert order is visible before inserting.
        public int OrdinalOf(Record r)
        {
            if (r == null) return 0;
            int k = _picks.IndexOf(r);
            return k < 0 ? 0 : k + 1;
        }

        // Test hook: measures the three bottom-bar entries with the same font and paddings
        // OnPaint uses, so "they never overlap the hint" is a number instead of a claim.
        public string BarProbe(int width)
        {
            using (var bmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bmp))
            {
                int n = _items.Count;
                int cnt = 0;
                for (int i = 0; i < n; i++) if (_checked[i]) cnt++;
                int used = 0;
                for (int i = 0; i < n; i++) if (_items[i].Used) used++;
                float bx = width - PadRight;
                float allW = g.MeasureString((n > 0 && cnt == n) ? "取消全选" : "全选", _fBar).Width;
                bx -= allW; bx -= 12f;
                float delW = g.MeasureString("删除 " + cnt + " 项", _fBar).Width;
                bx -= delW; bx -= 12f;
                float clrW = g.MeasureString("清理已引用(" + used + ")", _fBar).Width;
                bx -= clrW;
                float room = bx - 14f - PadLeft;
                // Worst case the record cap allows: 500 ticked rows and 500 used rows, i.e. the
                // widest the three entries can ever get.
                float worstDel = g.MeasureString("删除 500 项", _fBar).Width;
                float worstClr = g.MeasureString("清理已引用(500)", _fBar).Width;
                float worstRoom = width - PadRight - allW - 12f - worstDel - 12f - worstClr - 14f - PadLeft;
                float hintW = g.MeasureString("已勾选 500/500 · 按热键插入", _fBar).Width;
                return "width=" + width + " all=" + allW.ToString("0.0") + " del=" + delW.ToString("0.0")
                    + " clr=" + clrW.ToString("0.0") + " groupLeft=" + bx.ToString("0.0")
                    + " hintRoom=" + room.ToString("0.0") + (room >= 16f ? " ok" : " TIGHT")
                    + " worstRoom=" + worstRoom.ToString("0.0") + (worstRoom >= 16f ? " ok" : " TIGHT")
                    + " hintW=" + hintW.ToString("0.0")
                    + (worstRoom >= hintW ? " hint-full" : " hint-cut");
            }
        }

        public Record Selected()
        {
            if (_sel < 0 || _sel >= _items.Count) return null;
            return _items[_sel];
        }

        public Record Sample(int i)
        {
            if (i < 0 || i >= _items.Count) return null;
            return _items[i];
        }

        // Clears the tick on the quotes that were just inserted, so the next Enter does not
        // insert the same text a second time.
        public void Uncheck(List<Record> items)
        {
            if (items == null || items.Count == 0) return;
            for (int i = 0; i < _items.Count && i < _checked.Length; i++)
            {
                if (items.Contains(_items[i])) _checked[i] = false;
            }
            for (int k = 0; k < items.Count; k++) _picks.Remove(items[k]);
            Invalidate();
        }

        bool HasUrl(int i)
        {
            return _items[i].Url != null && _items[i].Url.Length > 0;
        }

        float BodyWidth { get { return Width - PadLeft - PadRight - CtrlZone; } }

        // A picture needs pixels in the list, not the word「图片」. Decoded thumbnails are cached
        // per file so scrolling a long list does not re-decode PNGs; the cache is dropped on
        // every reload because files can be deleted underneath it.
        const int MiniThumb = 40;
        const int MaxThumbW = 240;
        const int MaxThumbH = 160;
        readonly System.Collections.Generic.Dictionary<string, Image> _thumbs
            = new System.Collections.Generic.Dictionary<string, Image>();

        void ClearThumbs()
        {
            foreach (System.Collections.Generic.KeyValuePair<string, Image> kv in _thumbs)
                if (kv.Value != null) kv.Value.Dispose();
            _thumbs.Clear();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) ClearThumbs();
            base.Dispose(disposing);
        }

        Image ThumbOf(Record r)
        {
            if (r == null || !r.HasImage) return null;
            Image cached;
            if (_thumbs.TryGetValue(r.Image, out cached)) return cached;
            Image made = null;
            try
            {
                string p = Store.ImagePath(r.Image);
                if (System.IO.File.Exists(p))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(p);
                    using (MemoryStream ms = new MemoryStream(bytes))
                    using (Image raw = Image.FromStream(ms))
                        made = new Bitmap(raw);
                }
            }
            catch { made = null; }
            _thumbs[r.Image] = made;
            return made;
        }

        // Collapsed rows give the picture a small square on the right, expanded rows give it
        // the full width under the text. Both reserve space in the layout, so nothing overlaps.
        float TextWidth(int i)
        {
            if (!_items[i].HasImage || _open[i]) return BodyWidth;
            return Math.Max(40f, BodyWidth - MiniThumb - 8);
        }

        float TextHeight(Graphics g, int i)
        {
            if (!_open[i]) return BodyLineH * 2;
            SizeF sz = g.MeasureString(_items[i].DisplayText.Trim(), _fBody, new SizeF(TextWidth(i), 0f));
            return Math.Max(BodyLineH * 2, sz.Height);
        }

        float ThumbHeight(Graphics g, int i)
        {
            Image img = ThumbOf(_items[i]);
            if (img == null || img.Width <= 0) return 0f;
            float w = Math.Min(BodyWidth, MaxThumbW);
            float h = w * img.Height / (float)img.Width;
            if (h > MaxThumbH) h = MaxThumbH;
            return h;
        }

        float RowBodyH(Graphics g, int i)
        {
            float textH = TextHeight(g, i);
            if (!_items[i].HasImage) return textH;
            if (!_open[i]) return Math.Max(textH, MiniThumb + 4);
            float th = ThumbHeight(g, i);
            return th <= 0f ? textH : textH + 6 + th;
        }

        void RebuildLayout(Graphics g)
        {
            int n = _items.Count;
            _y = new float[n];
            _h = new float[n];
            _bodyH = new float[n];
            _groupNames.Clear();
            _groupY.Clear();
            float cur = 0f;
            string last = null;
            DateTimeOffset now = DateTimeOffset.Now;
            for (int i = 0; i < n; i++)
            {
                string grp = Records.GroupOf(_items[i], now);
                if (last == null || grp != last)
                {
                    _groupNames.Add(grp);
                    _groupY.Add(cur);
                    cur += GroupHeight;
                    last = grp;
                }
                _y[i] = cur;
                _bodyH[i] = RowBodyH(g, i);
                _h[i] = BodyTop + _bodyH[i] + 4 + MetaH + (_open[i] && HasUrl(i) ? UrlH : 0) + RowPadBottom;
                cur += _h[i];
            }
            _total = cur;
        }

        int ContentHeight { get { return Height - BottomBarH; } }

        void ClampScroll()
        {
            int ch = ContentHeight;
            if (_total <= ch) { _scroll = 0; return; }
            int max = (int)(_total - ch);
            if (_scroll < 0) _scroll = 0;
            if (_scroll > max) _scroll = max;
        }

        // Two visible lines, ellipsis on the second when the text is longer.
        static string FitPrefix(Graphics g, Font f, string s, float width, out int used, out bool cut)
        {
            if (g.MeasureString(s, f).Width <= width)
            {
                used = s.Length;
                cut = false;
                return s;
            }
            int lo = 0;
            int hi = s.Length - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (g.MeasureString(s.Substring(0, mid), f).Width <= width) lo = mid;
                else hi = mid - 1;
            }
            used = Math.Max(1, lo);
            cut = true;
            return s.Substring(0, used);
        }

        static string TwoLines(Graphics g, string s, Font f, float width)
        {
            string flat = Flatten(s);
            if (flat.Length == 0) return "";
            int used;
            bool cut;
            string l1 = FitPrefix(g, f, flat, width, out used, out cut);
            if (!cut) return l1;
            string rest = flat.Substring(used).TrimStart();
            if (rest.Length == 0) return l1;
            int used2;
            bool cut2;
            string l2 = FitPrefix(g, f, rest, width, out used2, out cut2);
            if (cut2)
            {
                int lo = 0;
                int hi = l2.Length;
                while (lo < hi)
                {
                    int mid = (lo + hi + 1) / 2;
                    if (g.MeasureString(l2.Substring(0, mid) + "…", f).Width <= width) lo = mid;
                    else hi = mid - 1;
                }
                l2 = l2.Substring(0, lo) + "…";
            }
            return l1 + "\n" + l2;
        }

        static string Flatten(string s)
        {
            StringBuilder sb = new StringBuilder(s.Length);
            bool space = false;
            foreach (char c in s)
            {
                if (char.IsWhiteSpace(c)) { space = true; continue; }
                if (space && sb.Length > 0) sb.Append(' ');
                space = false;
                sb.Append(c);
            }
            return sb.ToString();
        }

        static GraphicsPath Rounded(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        Rectangle CheckRect(int i)
        {
            return new Rectangle(Width - PadRight - 44, (int)_y[i] + BodyTop + 2 - _scroll, 16, 16);
        }

        Rectangle ArrowRect(int i)
        {
            return new Rectangle(Width - PadRight - 20, (int)_y[i] + BodyTop + 2 - _scroll, 16, 16);
        }

        // The URL sits on its own line under the source line when a quote is expanded.
        Rectangle UrlRect(int i)
        {
            int y = (int)(_y[i] + BodyTop + _bodyH[i] + 4 + MetaH) - _scroll;
            return new Rectangle(PadLeft, y, (int)BodyWidth, UrlH);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Theme.PanelBg);   // 先整面擦除再画板，否则拉伸时旧像素会留成拖影
            RebuildLayout(g);
            ClampScroll();

            // 内容板：圆角 + 顶部 1px 内亮线（浅色白、深色白 30），并把行内容裁进圆角
            RectangleF plateRect = new RectangleF(0f, 0f, Width, ContentHeight);
            using (GraphicsPath plate = Theme.RoundPath(plateRect, 18f))
            {
                using (var pb = new SolidBrush(Theme.DeepBg)) g.FillPath(pb, plate);
                g.SetClip(plate, CombineMode.Replace);
            }
            using (var hp = new Pen(Theme.IsLight ? Color.FromArgb(215, 255, 255, 255) : Color.FromArgb(30, 255, 255, 255), 1f))
                g.DrawLine(hp, 18f, 0.5f, Width - 18f, 0.5f);

            int ch = ContentHeight;
            int n = _items.Count;
            DateTimeOffset now = DateTimeOffset.Now;

            if (n == 0)
            {
                using (var b = new SolidBrush(Theme.TextMutedSoft))
                    g.DrawString("还没有记录。选中文字后按热键。", _fBody, b, PadLeft, 40f);
            }

            for (int gi = 0; gi < _groupNames.Count; gi++)
            {
                float gy = _groupY[gi] - _scroll;
                if (gy + GroupHeight < 0 || gy > ch) continue;
                using (var b = new SolidBrush(Theme.GroupLabel))
                    g.DrawString(_groupNames[gi], _fGroup, b, PadLeft, gy + 8f);
            }

            for (int i = 0; i < n; i++)
            {
                float y = _y[i] - _scroll;
                if (y + _h[i] < 0 || y > ch) continue;
                bool selected = i == _sel;
                Rectangle box = new Rectangle(PadLeft - 8, (int)y, Width - 2 * (PadLeft - 8), (int)_h[i] - 4);
                using (GraphicsPath path = Rounded(box, 10))
                {
                    using (var b = new SolidBrush(selected ? Theme.RowActive : Theme.RowIdle))
                        g.FillPath(b, path);
                }

                string body = _open[i]
                    ? _items[i].DisplayText.Trim()
                    : TwoLines(g, _items[i].DisplayText.Trim(), _fBody, TextWidth(i));
                using (var b = new SolidBrush(Theme.RowText))
                    g.DrawString(body, _fBody, b, new RectangleF(PadLeft, y + BodyTop, TextWidth(i), _bodyH[i]));

                // The picture itself, so a quote of an image is recognisable without opening it.
                Image thumb = ThumbOf(_items[i]);
                if (thumb != null)
                {
                    if (_open[i])
                    {
                        float textH = TextHeight(g, i);
                        float tw = Math.Min(BodyWidth, MaxThumbW);
float th = Math.Min(tw * thumb.Height / (float)thumb.Width, MaxThumbH);
                        if (tw < 8f) tw = 8f;
                        if (th > MaxThumbH) { th = MaxThumbH; tw = th * thumb.Width / (float)thumb.Height; }
                        g.DrawImage(thumb, new Rectangle(PadLeft, (int)(y + BodyTop + textH + 6), (int)tw, (int)th));
                    }
                    else
                    {
                        g.DrawImage(thumb, new Rectangle((int)(PadLeft + BodyWidth - MiniThumb),
                            (int)(y + BodyTop + 2), MiniThumb, MiniThumb));
                    }
                }

                float metaY = y + BodyTop + _bodyH[i] + 4;
                string meta = _items[i].Source + " · " + Records.Stamp(_items[i], now)
                    + (_items[i].Used ? " · 已引用" : "");
                using (var b = new SolidBrush(Theme.TextMutedSoft))
                    g.DrawString(meta, _fMeta, b, PadLeft, metaY);

                // Expanded rows also show where the quote came from, and a click copies it.
                if (_open[i] && HasUrl(i))
                {
                    float urlY = metaY + MetaH;
                    int used;
                    bool cut;
                    string shown = FitPrefix(g, _fMeta, _items[i].Url, BodyWidth, out used, out cut);
                    if (cut) shown = shown + "…";
                    using (var b = new SolidBrush(Theme.UrlText))
                        g.DrawString(shown, _fMeta, b, PadLeft, urlY);
                }

                DrawCheck(g, Width - PadRight - 44, y + BodyTop + 2, _checked[i]);
                // Ordinal of the tick, so the insert order is readable in the drawer.
                // Right of the text, left of the tick box (user rule m01787).
                if (_checked[i])
                {
                    int ord = OrdinalOf(_items[i]);
                    if (ord > 0)
                    {
                        string os = ord.ToString();
                        SizeF osz = g.MeasureString(os, _fOrd);
                        using (var b = new SolidBrush(Theme.OrdinalText))
                            g.DrawString(os, _fOrd, b, Width - PadRight - 52 - osz.Width, y + BodyTop + 1);
                    }
                }
                DrawArrow(g, Width - PadRight - 12, y + BodyTop + 10, _open[i]);
            }

            // scrollbar
            if (_total > ch)
            {
                int trackH = ch - 12;
                if (trackH > 20)
                {
                    int thumbH = Math.Max(28, (int)(trackH * (ch / _total)));
                    int maxScroll = Math.Max(1, (int)(_total - ch));
                    int thumbY = 6 + (int)((trackH - thumbH) * (_scroll / (float)maxScroll));
                    using (var b = new SolidBrush(Theme.ScrollThumb))
                        g.FillRectangle(b, Width - 7, thumbY, 4, thumbH);
                }
            }

            // bottom bar
            int barY = Height - BottomBarH;
            using (var b = new SolidBrush(Theme.BottomBar))
            using (GraphicsPath barPath = Theme.BottomRounded(new RectangleF(0f, barY, Width, BottomBarH), 18f))
                g.FillPath(b, barPath);
            int cnt = 0;
            for (int i = 0; i < n; i++) if (_checked[i]) cnt++;
            int usedCount = 0;
            for (int i = 0; i < n; i++) if (_items[i].Used) usedCount++;

            _clearRect = RectangleF.Empty;
            _delRect = RectangleF.Empty;
            _allRect = RectangleF.Empty;

            // The three controls are laid out right to left and painted unconditionally, so
            // ticking a row never reflows the bar: only the enabled state changes. Order on
            // screen (left to right): 清理已引用(N) | 删除 N 项 | 全选.
            float bx = Width - PadRight;

            string all = (n > 0 && cnt == n) ? "取消全选" : "全选";
            SizeF asz = g.MeasureString(all, _fBar);
            bx -= asz.Width;
            using (var b = new SolidBrush(n > 0 ? Theme.HintText : Theme.TextDisabled))
                g.DrawString(all, _fBar, b, bx, barY + 9f);
            if (n > 0) _allRect = new RectangleF(bx - 6f, barY + 3f, asz.Width + 12f, BottomBarH - 5f);
            bx -= 12f;

            string del = "删除 " + cnt + " 项";
            SizeF dsz = g.MeasureString(del, _fBar);
            bx -= dsz.Width;
            using (var b = new SolidBrush(cnt > 0 ? Theme.DangerText : Theme.TextDisabled))
                g.DrawString(del, _fBar, b, bx, barY + 9f);
            if (cnt > 0) _delRect = new RectangleF(bx - 6f, barY + 3f, dsz.Width + 12f, BottomBarH - 5f);
            bx -= 12f;

            string clr = "清理已引用(" + usedCount + ")";
            SizeF csz = g.MeasureString(clr, _fBar);
            bx -= csz.Width;
            using (var b = new SolidBrush(usedCount > 0 ? Theme.HintText : Theme.TextDisabled))
                g.DrawString(clr, _fBar, b, bx, barY + 9f);
            if (usedCount > 0) _clearRect = new RectangleF(bx - 6f, barY + 3f, csz.Width + 12f, BottomBarH - 5f);

            // Whatever room is left goes to the hint, and a narrow window shortens the words
            // instead of letting them run under the controls.
            string bar = _hint != null
                ? _hint
                : (cnt > 0
                    ? ("已勾选 " + cnt + "/" + n + " · 按热键插入")
                    : "未勾选 · 勾选后按热键插入");
            Color barColor = _hint != null ? Theme.Warn : Theme.BarHint;
            float room = bx - 14f - PadLeft;
            if (room > 16f)
            {
                int usedChars;
                bool cutChars;
                string shown = FitPrefix(g, _fBar, bar, room, out usedChars, out cutChars);
                using (var b = new SolidBrush(barColor))
                    g.DrawString(shown, _fBar, b, PadLeft, barY + 9f);
            }
        }

        void DrawArrow(Graphics g, float cx, float cy, bool up)
        {
            using (var pen = new Pen(Color.FromArgb(160, 166, 180), 1.6f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                if (up)
                {
                    g.DrawLine(pen, cx - 5f, cy + 3f, cx, cy - 3f);
                    g.DrawLine(pen, cx, cy - 3f, cx + 5f, cy + 3f);
                }
                else
                {
                    g.DrawLine(pen, cx - 5f, cy - 3f, cx, cy + 3f);
                    g.DrawLine(pen, cx, cy + 3f, cx + 5f, cy - 3f);
                }
            }
        }

        void DrawCheck(Graphics g, float x, float y, bool on)
        {
            using (GraphicsPath path = Rounded(new Rectangle((int)x, (int)y, 16, 16), 4))
            {
                using (var b = new SolidBrush(on ? Color.FromArgb(79, 124, 255) : Color.FromArgb(46, 48, 56)))
                    g.FillPath(b, path);
                using (var pen = new Pen(Theme.TextSoft, 1f))
                    g.DrawPath(pen, path);
            }
            if (!on) return;
            using (var pen = new Pen(Color.White, 2f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLine(pen, x + 4f, y + 8.5f, x + 7f, y + 11.5f);
                g.DrawLine(pen, x + 7f, y + 11.5f, x + 12f, y + 5f);
            }
        }

        int HitRow(int y)
        {
            int ly = y + _scroll;
            for (int i = 0; i < _items.Count; i++)
                if (ly >= _y[i] && ly < _y[i] + _h[i]) return i;
            return -1;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            using (Graphics g = CreateGraphics()) { RebuildLayout(g); }
            if (e.Y > ContentHeight)
            {
                // The bottom bar is a control surface, not just a caption.
                if (!_delRect.IsEmpty && _delRect.Contains(e.X, e.Y))
                {
                    EventHandler d = DeleteRequested;
                    if (d != null) d(this, EventArgs.Empty);
                    return;
                }
                if (!_allRect.IsEmpty && _allRect.Contains(e.X, e.Y))
                {
                    ToggleSelectAll();
                    return;
                }
                if (!_clearRect.IsEmpty && _clearRect.Contains(e.X, e.Y))
                {
                    EventHandler h = CleanupRequested;
                    if (h != null) h(this, EventArgs.Empty);
                }
                return;
            }
            int i = HitRow(e.Y);
            if (i < 0) return;
            if (CheckRect(i).Contains(e.X, e.Y))
            {
                Toggle(i);
            }
            else if (_open[i] && HasUrl(i) && UrlRect(i).Contains(e.X, e.Y))
            {
                // Copying the URL is an explicit click, so overwriting the clipboard here is
                // the user's own action; the hint says so.
                _sel = i;
                _hint = Native.SetClipboardText(_items[i].Url) ? "网址已复制" : "复制网址失败";
            }
            else if (ArrowRect(i).Contains(e.X, e.Y))
            {
                _open[i] = !_open[i];
                _sel = i;
                _hint = null;
            }
            else
            {
                _sel = i;
                _hint = null;
            }
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            _scroll -= e.Delta / 3;
            ClampScroll();
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            Keys k = keyData & Keys.KeyCode;
            if (k == Keys.Up || k == Keys.Down || k == Keys.Left || k == Keys.Right
                || k == Keys.Space || k == Keys.Enter || k == Keys.PageUp || k == Keys.PageDown)
                return true;
            return base.IsInputKey(keyData);
        }

        void MoveTo(int i)
        {
            if (_items.Count == 0) return;
            if (i < 0) i = 0;
            if (i > _items.Count - 1) i = _items.Count - 1;
            _sel = i;
            _hint = null;
            EnsureVisible(i);
            Invalidate();
        }

        void EnsureVisible(int i)
        {
            using (Graphics g = CreateGraphics()) { RebuildLayout(g); }
            int ch = ContentHeight;
            float top = _y[i];
            float bottom = _y[i] + _h[i];
            if (top - _scroll < 0) _scroll = (int)top;
            else if (bottom - _scroll > ch) _scroll = (int)(bottom - ch);
            ClampScroll();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                if (CloseRequested != null) CloseRequested(this, EventArgs.Empty);
                return;
            }
            if (_items.Count == 0) return;
            switch (e.KeyCode)
            {
                case Keys.Up: e.Handled = true; MoveTo(_sel - 1); break;
                case Keys.Down: e.Handled = true; MoveTo(_sel + 1); break;
                case Keys.PageUp: e.Handled = true; MoveTo(_sel - 5); break;
                case Keys.PageDown: e.Handled = true; MoveTo(_sel + 5); break;
                case Keys.Home: e.Handled = true; MoveTo(0); break;
                case Keys.End: e.Handled = true; MoveTo(_items.Count - 1); break;
                case Keys.Space:
                    e.Handled = true;
                    if (_sel >= 0) Toggle(_sel);
                    break;
                case Keys.Left:
                    e.Handled = true;
                    if (_sel >= 0) { _open[_sel] = false; _hint = null; Invalidate(); }
                    break;
                case Keys.Right:
                    e.Handled = true;
                    if (_sel >= 0) { _open[_sel] = true; _hint = null; Invalidate(); }
                    break;
                case Keys.Enter:
                    // Enter no longer inserts: the insert is the hotkey only, so the same
                    // key never means two different things depending on where the focus is.
                    e.Handled = true;
                    _hint = "插入请按热键（" + FloatBar.HotkeyLabel + "）";
                    Invalidate();
                    break;
            }
        }
    }

    // Batch removal of quotes that were inserted at least once. Plain controls on
    // purpose: this panel is destructive, so it should look like a system dialog and
    // not like part of the appealing drawer.
    class CleanupForm : Form
    {
        readonly List<Record> _items;
        readonly List<CheckBox> _boxes = new List<CheckBox>();
        readonly Label _tip = new Label();

        public int Deleted { get; private set; }

        public CleanupForm(List<Record> items)
        {
            _items = items;
            Text = "清理已引用的内容";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            ClientSize = new Size(440, 470);
            BackColor = Theme.PanelBg;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Microsoft YaHei UI", 9.5f);

            Panel panel = new Panel();
            panel.Location = new Point(14, 12);
            panel.Size = new Size(412, 370);
            panel.AutoScroll = true;
            panel.BackColor = Theme.PanelBg;
            Controls.Add(panel);

            int y = 2;
            if (_items.Count == 0)
            {
                Label empty = new Label();
                empty.Text = "还没有插入过的内容。";
                empty.ForeColor = Theme.TextMuted;
                empty.AutoSize = true;
                empty.Location = new Point(4, 8);
                panel.Controls.Add(empty);
            }
            foreach (Record r in _items)
            {
                CheckBox cb = new CheckBox();
                string body = r.DisplayText.Replace("\r", " ").Replace("\n", " ").Trim();
                if (body.Length > 34) body = body.Substring(0, 34) + "…";
                cb.Text = Records.Stamp(r, DateTimeOffset.Now) + "  ·  " + Records.SrcLabel(r) + "\n" + body;
                cb.Tag = r;
                cb.Checked = true;
                cb.AutoSize = false;
                cb.Size = new Size(384, 46);
                cb.Location = new Point(2, y);
                cb.ForeColor = Theme.TextPrimary;
                cb.BackColor = Theme.PanelBg;
                cb.FlatStyle = FlatStyle.Flat;
                panel.Controls.Add(cb);
                _boxes.Add(cb);
                y += 50;
            }

            _tip.AutoSize = false;
            _tip.Size = new Size(412, 22);
            _tip.Location = new Point(14, 390);
            _tip.ForeColor = Theme.TextMuted;
            _tip.Text = _items.Count + " 条已引用内容";
            Controls.Add(_tip);

            Button del = new Button();
            del.Text = "删除选中";
            del.Size = new Size(130, 32);
            del.Location = new Point(14, 420);
            del.FlatStyle = FlatStyle.Flat;
            del.ForeColor = Theme.DangerText;
            del.BackColor = Theme.DangerField;
            del.Click += delegate { DeleteChecked(); };
            Controls.Add(del);

            Button cancel = new Button();
            cancel.Text = "取消";
            cancel.Size = new Size(92, 32);
            cancel.Location = new Point(ClientSize.Width - 92 - 14, 420);
            cancel.FlatStyle = FlatStyle.Flat;
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);
            CancelButton = cancel;
        }

        void DeleteChecked()
        {
            HashSet<string> drop = new HashSet<string>(StringComparer.Ordinal);
            int n = 0;
            foreach (CheckBox cb in _boxes)
            {
                if (!cb.Checked) continue;
                Record r = (Record)cb.Tag;
                drop.Add(r.Key);
                n++;
            }
            if (n == 0)
            {
                _tip.Text = "没有勾选内容";
                return;
            }
            DialogResult ask = MessageBox.Show(this,
                "删除 " + n + " 条已引用内容？删除后不可恢复。", "确认删除",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (ask != DialogResult.Yes) return;

            int removed;
            string err;
            if (!Store.DeleteByKey(drop, out removed, out err))
            {
                _tip.Text = "删除失败：" + err;
                return;
            }
            Deleted = removed;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    // The round button that floats beside the drawer. It is a separate window on purpose: a
    // child control would be clipped by the drawer frame, and this button has to stick out past
    // the panel's edge. WS_EX_NOACTIVATE keeps the drawer's focus, so opening settings cannot
    // silently change what the hotkey would do next (the routing checks ContainsFocus).
    class GearBubble : Form
    {
        const int D = 36;
        bool _hover;

        public event EventHandler Clicked;

        public GearBubble()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(D, D);
            BackColor = Theme.RowActive;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
| ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            ApplyRegion();
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                return cp;
            }
        }

        // Round window: the corners are cut away so a click there falls through to whatever is
        // behind the button instead of landing on an invisible square.
        void ApplyRegion()
        {
            Region old = Region;
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddEllipse(0, 0, D - 1, D - 1);
                Region = new Region(p);
            }
            if (old != null) old.Dispose();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF box = new RectangleF(0.5f, 0.5f, D - 2, D - 2);
            using (SolidBrush b = new SolidBrush(_hover ? Theme.RowHover : Theme.RowActive))
                g.FillEllipse(b, box);
            using (Pen ring = new Pen(Theme.GearRing))
                g.DrawEllipse(ring, box);

            // Gear: eight teeth around a ring plus a hub, drawn as lines so no icon file or
            // font glyph has to ship with the app.
            float cx = D / 2f;
            float cy = D / 2f;
            Color ink = _hover ? Theme.TextPrimary : Theme.TextBody;
            using (Pen p = new Pen(ink, 1.6f))
            {
                for (int i = 0; i < 8; i++)
                {
                    double a = i * Math.PI / 4.0;
                    float x1 = cx + (float)(Math.Cos(a) * 8.0);
                    float y1 = cy + (float)(Math.Sin(a) * 8.0);
                    float x2 = cx + (float)(Math.Cos(a) * 11.5);
                    float y2 = cy + (float)(Math.Sin(a) * 11.5);
                    g.DrawLine(p, x1, y1, x2, y2);
                }
                g.DrawEllipse(p, cx - 7f, cy - 7f, 14f, 14f);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;
            EventHandler h = Clicked;
            if (h != null) h(this, EventArgs.Empty);
        }
    }

    // Hotkey editor. Saving no longer means "find config.json and restart by hand": this panel
    // writes the file and can restart the app, releasing the old hotkey registration first —
    // a replacement instance on the same combo would otherwise fail with
    // ERROR_HOTKEY_ALREADY_REGISTERED (1409).
    // Records the chord instead of asking the user to type it: click into the box, press the keys,
    // done. Clicking the box again always re-arms it, so a second attempt replaces the first
    // instead of silently keeping it. Capture is driven by SettingsForm.ProcessCmdKey, because
    // Alt combinations arrive as WM_SYSKEYDOWN and never reach a control's OnKeyDown — that was
    // why a combination containing Alt could not be recorded a second time.
    class HotkeyBox : TextBox
    {
        public const string PromptText = "请按下组合键…（Esc 取消、Delete 清空）";

        string _value = "";
        string _backup;
        bool _armed;

        public HotkeyBox()
        {
            ReadOnly = true;
            ShortcutsEnabled = false;
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        public event EventHandler ValueCaptured;

        public string Value
        {
            get { return _value; }
            set
            {
                _value = value == null ? "" : value;
                _backup = _value;
                _armed = false;
                Text = _value;
            }
        }

        public bool Armed
        {
            get { return _armed; }
        }

        // Puts the box back into "waiting for a chord" state and shows the prompt. The old value
        // is kept as a backup so Escape can restore it.
        public void Arm()
        {
            _armed = true;
            _backup = _value;
            Text = PromptText;
            SelectAll();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Arm();
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            if (!_armed) Arm();
        }

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            // Leaving does not throw the recording away: the box shows the current value and stays
            // armed, so clicking it again (or pressing a key after Tab) still records.
            Text = _value;
        }

        // Returns true when the press belongs to the recording and must not reach anything else.
        public bool CaptureKey(Keys keyData)
        {
            Keys k = keyData & Keys.KeyCode;
            Keys mods = keyData & Keys.Modifiers;

            if (k == Keys.Escape)
            {
                _value = _backup == null ? _value : _backup;
                Done();
                return true;
            }
            if (k == Keys.Back || k == Keys.Delete)
            {
                _value = "";
                Done();
                return true;
            }
            if (k == Keys.ControlKey || k == Keys.ShiftKey || k == Keys.Menu
                || k == Keys.LWin || k == Keys.RWin)
            {
                Text = "按住修饰键，再按主键…";
                return true;
            }

            string text;
            string error;
            if (!Hotkey.FromKeys(k, mods, out text, out error))
            {
                Text = error;
                return true;
            }
            _value = text;
            Done();
            return true;
        }

        void Done()
        {
            _armed = true;
            Text = _value;
            if (ValueCaptured != null) ValueCaptured(this, EventArgs.Empty);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Fallback for keys the form does not route (it handles everything while this control
            // has focus), kept so the control still works if used on its own.
            e.Handled = true;
            e.SuppressKeyPress = true;
            CaptureKey(e.KeyData);
        }
    }

    // One collapsible section: title on the left, current value and a chevron on the right. The panel
    // has a single section for now; a section's body lives in its own panel, so another section can be
    // added later without touching this drawing code.
    class AccordionRow : Control
    {
        string _title = "";
        string _valueText = "";
        bool _expanded;

        public event EventHandler Toggled;

        public AccordionRow()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Height = 46;
            BackColor = Theme.FieldBg;
        }

        public string Title
        {
            get { return _title; }
            set { _title = value == null ? "" : value; Invalidate(); }
        }

        public string ValueText
        {
            get { return _valueText; }
            set { _valueText = value == null ? "" : value; Invalidate(); }
        }

        public bool Expanded
        {
            get { return _expanded; }
            set { _expanded = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            Rectangle box = new Rectangle(0, 0, Width - 1, Height - 1);
            using (Pen border = new Pen(Theme.SettingsBorder))
                g.DrawRectangle(border, box);

            using (Font f = new Font("Microsoft YaHei UI", 10f))
            using (SolidBrush textBrush = new SolidBrush(Theme.SettingsText))
                g.DrawString(_title, f, textBrush, new PointF(14f, (Height - f.Height) / 2f - 1f));

            if (_valueText.Length > 0)
            {
                using (Font small = new Font("Microsoft YaHei UI", 8.5f))
                    TextRenderer.DrawText(g, _valueText, small,
                        new Rectangle(140, 0, Width - 140 - 30, Height),
                        Theme.SettingsMuted,
                        TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            int cx = Width - 20;
            int cy = Height / 2;
            using (Pen chevron = new Pen(Theme.SettingsArrow, 1.8f))
            {
                if (_expanded)
                    g.DrawLines(chevron, new Point[] { new Point(cx - 5, cy - 2), new Point(cx, cy + 3), new Point(cx + 5, cy - 2) });
                else
                    g.DrawLines(chevron, new Point[] { new Point(cx - 2, cy - 5), new Point(cx + 3, cy), new Point(cx - 2, cy + 5) });
            }
        }

        // The row toggles itself; the form listens to Toggled to resize and show the body.
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;
            _expanded = !_expanded;
            Invalidate();
            if (Toggled != null) Toggled(this, EventArgs.Empty);
        }
    }
    class SettingsForm : Form
    {
        readonly HotkeyBox _box = new HotkeyBox();
        readonly Label _msg = new Label();
        readonly AccordionRow _row = new AccordionRow();
        bool _bodyOpen;
        readonly Panel _body = new Panel();
        Button _save;
        Button _both;

        public string Saved { get; private set; }

        public bool RestartRequested { get; private set; }

        public SettingsForm(string current)
        {
            Text = "QuoteStack 设置";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Theme.PanelBg;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Microsoft YaHei UI", 9.5f);

            _row.Title = "热键修改";
            _row.ValueText = current == null ? "" : current;
            _row.Location = new Point(16, 14);
            _row.Size = new Size(340, 46);
            _row.Toggled += delegate { ApplyLayout(_row.Expanded); };
            Controls.Add(_row);

            _body.Location = new Point(16, 66);
            _body.Size = new Size(340, 132);
            _body.BackColor = BackColor;
            Controls.Add(_body);

            _box.Value = current == null ? "" : current;
            _box.Location = new Point(0, 0);
            _box.Size = new Size(340, 26);
            _box.BackColor = Theme.FieldBg;
            _box.ForeColor = Theme.TextPrimary;
            _box.BorderStyle = BorderStyle.FixedSingle;
            _box.ValueCaptured += delegate { _row.ValueText = _box.Value; };
            _body.Controls.Add(_box);

            Label hint = new Label();
            hint.Text = "点这里后直接按组合键（Esc 取消）· Alt+Q 常被豆包占用，建议 Ctrl+Alt+Q";
            hint.AutoSize = false;
            hint.Size = new Size(340, 18);
            hint.Location = new Point(0, 34);
            hint.ForeColor = Theme.TextMuted;
            hint.Font = new Font("Microsoft YaHei UI", 8.5f);
            _body.Controls.Add(hint);

            // Same strip as the hint: validation errors replace the reminder instead of appearing
            // somewhere the user is not looking.
            _msg.AutoSize = false;
            _msg.Size = new Size(340, 18);
            _msg.Location = new Point(0, 34);
            _msg.ForeColor = Theme.Danger;
            _msg.Font = new Font("Microsoft YaHei UI", 8.5f);
            _body.Controls.Add(_msg);
            _msg.BringToFront();

            _save = MkButton("保存", 0, 64, 96);
            _save.Click += delegate { SavePressed(false); };
            _body.Controls.Add(_save);

            _both = MkButton("保存并重启", 104, 64, 118);
            _both.Click += delegate { SavePressed(true); };
            _body.Controls.Add(_both);

            Button cancel = MkButton("取消", 230, 64, 96);
            cancel.DialogResult = DialogResult.Cancel;
            _body.Controls.Add(cancel);

            CancelButton = cancel;
            AcceptButton = _both;

            ApplyLayout(false);
        }

        // Collapsed by default: the dialog opens as a one-row list, and expanding the row is what
        // reveals the recorder. Height is set explicitly so the hidden body leaves no gap.
        void ApplyLayout(bool expanded)
        {
            _row.Expanded = expanded;
            _bodyOpen = expanded;
            _body.Visible = expanded;
            AcceptButton = expanded ? _both : null;
            ClientSize = new Size(372, expanded ? 212 : 74);
        }

        internal AccordionRow FirstRow { get { return _row; } }

        // Control.Visible reports false while the whole form is still hidden, so the layout state is
        // tracked separately: that is what the headless test can assert on.
        internal bool BodyVisible { get { return _bodyOpen; } }

        public void ToggleFirstRow()
        {
            ApplyLayout(!_row.Expanded);
        }

        // Every key goes through here while the recording box has focus, because Alt combinations
        // arrive as WM_SYSKEYDOWN and can be swallowed by the window manager before a control's
        // OnKeyDown sees them. Tab / Enter are passed on so the buttons keep working normally.
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys code = keyData & Keys.KeyCode;
            if (_box.Focused && code != Keys.Tab && code != Keys.Enter)
            {
                if (!_box.Armed) _box.Arm();
                _box.CaptureKey(keyData);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Escape closes the dialog even while it is collapsed (the cancel button lives inside the
        // hidden body). Esc inside the recorder is consumed earlier by HotkeyBox, so it still only
        // undoes the recording there.
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }

        Button MkButton(string text, int x, int y, int w)
        {
            Button b = new Button();
            b.Text = text;
            b.Size = new Size(w, 32);
            b.Location = new Point(x, y);
            b.FlatStyle = FlatStyle.Flat;
            b.ForeColor = Theme.TextPrimary;
            b.BackColor = Theme.RowActive;
            return b;
        }

        void SavePressed(bool restart)
        {
            string text = _box.Value == null ? "" : _box.Value.Trim();
            uint mods;
            uint vk;
            string error;
            if (!Hotkey.Parse(text, out mods, out vk, out error))
            {
                // A refused value is exactly the moment a user asks "why did nothing happen?",
                // so it is recorded. The hotkey itself is not private (no page text, no path).
                Store.LogStartup("settings: hotkey-rejected value=[" + text + "] reason=" + error);
                _msg.Text = error;
                return;
            }
            try
            {
                Config.SaveHotkey(text);
            }
            catch (Exception ex)
            {
                Store.LogStartup("settings: save-failed value=[" + text + "] ex=" + ex.GetType().Name);
                _msg.Text = "写入失败：" + ex.GetType().Name + "（" + ex.Message + "）";
                return;
            }
            Store.LogStartup("settings: hotkey=" + text + " restart=" + (restart ? "yes" : "no"));
            Saved = text;
            RestartRequested = restart;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    // A drop carries whatever the source program chose to put there: plain text, a bitmap,
    // real files on disk, or — for a picture dragged out of a browser — a file group whose
    // bytes are streamed on demand. Everything is normalised into one payload here, and the
    // caller writes at most one record per picture: one drop never becomes two quotes for the
    // same picture, no matter how many formats the source offered.
    internal static class DragIn
    {
        public const int MaxImageBytes = 20 * 1024 * 1024;
        // A browser can still be producing the bytes while the drop happens; the read stops
        // after this budget instead of freezing the UI thread (asked for explicitly).
        const int ReadBudgetMs = 3000;

        public delegate void Handler(Payload payload);

        public sealed class Payload
        {
            public readonly List<byte[]> Images = new List<byte[]>();
            public string Text;
            public string Url;
            public string Formats = "";
            // Raw markup from the drag, kept in memory only: page drags carry no pixels, just
            // the <img src> address that a picture can be fetched from.
            public string Html;
        }

        // Decides the cursor ("copy" or "no entry"), so it must be pessimistic: a file drop
        // that contains no picture is refused here rather than accepted and then failed.
        public static bool Supported(System.Windows.Forms.IDataObject d)
        {
            if (d == null) return false;
            if (Has(d, DataFormats.Bitmap)) return true;
            if (Has(d, "FileGroupDescriptorW") || Has(d, "FileGroupDescriptor")) return true;
            if (Has(d, DataFormats.FileDrop))
            {
                try
                {
                    string[] files = d.GetData(DataFormats.FileDrop) as string[];
                    if (files != null)
                        for (int i = 0; i < files.Length; i++)
                            if (IsImageFile(files[i])) return true;
                }
                catch { }
            }
            return Has(d, DataFormats.UnicodeText) || Has(d, DataFormats.Text);
        }

        static bool Has(System.Windows.Forms.IDataObject d, string format)
        {
            try { return d.GetDataPresent(format); } catch { return false; }
        }

        public static Payload Read(System.Windows.Forms.IDataObject d)
        {
            Payload p = new Payload();
            if (d == null) return p;
            p.Formats = FormatList(d);
            p.Html = HtmlOf(d);
            p.Url = UrlOf(d);
            p.Text = TextOf(d);
            List<byte[]> images = ImagesOf(d);
            for (int i = 0; i < images.Count; i++) p.Images.Add(images[i]);
            return p;
        }

        static string FormatList(System.Windows.Forms.IDataObject d)
        {
            string[] names = new string[]
            {
                "Bitmap", "FileDrop", "FileGroupDescriptorW", "FileGroupDescriptor",
                "FileContents", "Html", "UriList", "UnicodeText", "Text"
            };
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < names.Length; i++)
            {
                string n = names[i];
                bool present =
                    n == "Html" ? Has(d, DataFormats.Html)
                    : n == "UriList" ? Has(d, "text/uri-list")
                    : Has(d, n);
                if (!present) continue;
                if (sb.Length > 0) sb.Append('|');
                sb.Append(n);
            }
            return sb.ToString();
        }

        // The page a picture came from. Browsers put a "SourceURL:" line in their HTML fragment;
        // when that is missing or not a real address the quote is labelled「拖拽」instead.
        static string HtmlOf(System.Windows.Forms.IDataObject d)
        {
            try { if (Has(d, DataFormats.Html)) return d.GetData(DataFormats.Html) as string; }
            catch { }
            return null;
        }

        static string UrlOf(System.Windows.Forms.IDataObject d)
        {
            string html = HtmlOf(d);
            if (html != null)
            {
                int at = html.IndexOf("SourceURL:", StringComparison.OrdinalIgnoreCase);
                if (at >= 0)
                {
                    int nl = html.IndexOf('\n', at);
                    string line = html.Substring(at + 10, (nl < 0 ? html.Length : nl) - (at + 10));
                    line = line.Trim().TrimEnd('\r');
                    if (Capture.LooksLikeUrl(line)) return line;
                }
            }
            try
            {
                if (Has(d, "text/uri-list"))
                {
                    string uris = d.GetData("text/uri-list") as string;
                    if (uris != null)
                    {
                        string[] one = uris.Split('\n');
                        for (int i = 0; i < one.Length; i++)
                        {
                            string u = one[i].Trim();
                            if (Capture.LooksLikeUrl(u)) return u;
                        }
                    }
                }
            }
            catch { }
            return "";
        }

        static string TextOf(System.Windows.Forms.IDataObject d)
        {
            try
            {
                if (Has(d, DataFormats.UnicodeText)) return d.GetData(DataFormats.UnicodeText) as string;
                if (Has(d, DataFormats.Text)) return d.GetData(DataFormats.Text) as string;
            }
            catch { }
            return null;
        }

        static List<byte[]> ImagesOf(System.Windows.Forms.IDataObject d)
        {
            List<byte[]> list = new List<byte[]>();
            // 1. a real bitmap (Word, WPS, Paint hand one over directly)
            if (Has(d, DataFormats.Bitmap))
            {
                try
                {
                    byte[] png = PngOf(d.GetData(DataFormats.Bitmap) as Image);
                    if (png != null) { list.Add(png); return list; }
                }
                catch { }
            }
            // 2. real files on disk (a saved picture, or a page that hands over a temp file)
            if (Has(d, DataFormats.FileDrop))
            {
                try
                {
                    string[] files = d.GetData(DataFormats.FileDrop) as string[];
                    if (files != null)
                    {
                        for (int i = 0; i < files.Length; i++)
                        {
                            if (!IsImageFile(files[i])) continue;
                            byte[] b = NormalizePng(ReadFileCapped(files[i], MaxImageBytes));
                            if (b != null) list.Add(b);
                        }
                    }
                }
                catch { }
                if (list.Count > 0) return list;
            }
            // 3. a virtual file: browsers stream the picture's bytes through FileContents
            if (Has(d, "FileGroupDescriptorW") || Has(d, "FileGroupDescriptor"))
            {
                System.Runtime.InteropServices.ComTypes.IDataObject com =
                    d as System.Runtime.InteropServices.ComTypes.IDataObject;
                if (com != null) ReadVirtual(com, list);
            }
            return list;
        }

        static bool IsImageFile(string path)
        {
            if (path == null) return false;
            string ext = Path.GetExtension(path);
            if (ext == null) return false;
            ext = ext.ToLowerInvariant();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp"
                || ext == ".gif" || ext == ".webp" || ext == ".tif" || ext == ".tiff";
        }

        static byte[] ReadFileCapped(string path, int maxBytes)
        {
            try
            {
                FileInfo fi = new FileInfo(path);
                if (!fi.Exists || fi.Length <= 0 || fi.Length > maxBytes) return null;
                return File.ReadAllBytes(path);
            }
            catch { return null; }
        }

        static byte[] PngOf(Image img)
        {
            if (img == null) return null;
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    return ms.ToArray();
                }
            }
            catch { return null; }
        }

        // Stored files are named .png, so anything that is not already a PNG is re-encoded.
        // ---- pictures that a page drag only points at -------------------------------------
        // Nothing here runs unless the user drags a text+image selection out of a browser: the
        // markup carries the picture's address but not its bytes, so that one address is fetched.
        // Declared in README: a single GET, no cookies, nothing about the app or the quotes is sent.

        public static byte[] FirstInlineImage(string html)
        {
            if (html == null) return null;
            Match m = Regex.Match(html,
                "src\\s*=\\s*\"data:image/[a-zA-Z0-9.+-]+;base64,([A-Za-z0-9+/=\\s]+)\"",
                RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            try
            {
                byte[] raw = Convert.FromBase64String(Regex.Replace(m.Groups[1].Value, "\\s+", ""));
                return NormalizePng(raw);
            }
            catch { return null; }
        }

        public static string FirstImageUrl(string html, string pageUrl)
        {
            if (html == null) return null;
            Match m = Regex.Match(html,
                "<img[^>]*?\\ssrc\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)'|([^\\s>]+))",
                RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            string src = m.Groups[1].Success ? m.Groups[1].Value
                : m.Groups[2].Success ? m.Groups[2].Value
                : m.Groups[3].Value;
            src = (src == null ? "" : src.Trim()).Replace("&amp;", "&");
            if (src.Length == 0) return null;
            if (src.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return null;
            if (src.StartsWith("//", StringComparison.Ordinal)) src = "https:" + src;
            Uri abs;
            if (!Uri.TryCreate(src, UriKind.Absolute, out abs))
            {
                Uri baseUri;
                if (pageUrl == null || !Uri.TryCreate(pageUrl, UriKind.Absolute, out baseUri)) return null;
                if (!Uri.TryCreate(baseUri, src, out abs)) return null;
            }
            if (abs.Scheme != Uri.UriSchemeHttp && abs.Scheme != Uri.UriSchemeHttps) return null;
            return abs.AbsoluteUri;
        }

        // Loopback, private, link-local and CGNAT addresses are refused before any request is
        // made, and the host is resolved first so a DNS name pointing inside is refused too.
        static bool PublicIp(System.Net.IPAddress ip)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                if (System.Net.IPAddress.IPv6Loopback.Equals(ip)) return false;
                if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast) return false;
                return true;
            }
            byte[] b = ip.GetAddressBytes();
            if (b[0] == 0 || b[0] == 10 || b[0] == 127) return false;
            if (b[0] == 169 && b[1] == 254) return false;
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return false;
            if (b[0] == 192 && b[1] == 168) return false;
            if (b[0] == 100 && b[1] >= 64 && b[1] <= 127) return false;
            if (b[0] >= 224) return false;
            return true;
        }

        static bool PublicHost(string host)
        {
            if (host == null || host.Length == 0) return false;
            string h = host.ToLowerInvariant();
            if (h == "localhost" || h.EndsWith(".local", StringComparison.Ordinal)) return false;
            try
            {
                System.Net.IPAddress[] addrs = Dns.GetHostAddresses(host);
                if (addrs == null || addrs.Length == 0) return false;
                for (int i = 0; i < addrs.Length; i++) if (!PublicIp(addrs[i])) return false;
                return true;
            }
            catch { return false; }
        }

        static bool BigEnough(byte[] png)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(png))
                using (Image img = Image.FromStream(ms))
                    return img.Width >= 16 && img.Height >= 16;
            }
            catch { return false; }
        }

        public static byte[] FetchImage(string url, int timeoutMs, int maxBytes, out string error)
        {
            error = "none";
            Uri uri;
            if (url == null || !Uri.TryCreate(url, UriKind.Absolute, out uri)) { error = "badurl"; return null; }
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) { error = "blocked"; return null; }
            if (!PublicHost(uri.Host)) { error = "blocked"; return null; }
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri);
                req.Method = "GET";
                req.Timeout = timeoutMs;
                req.ReadWriteTimeout = timeoutMs;
                // A redirect could aim at a private address after the check above.
                req.AllowAutoRedirect = false;
                req.UserAgent = "QuoteStack";
                req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                req.CookieContainer = new CookieContainer();
                using (HttpWebResponse res = (HttpWebResponse)req.GetResponse())
                {
                    if (res.StatusCode != HttpStatusCode.OK) { error = "http-" + (int)res.StatusCode; return null; }
                    if (res.ContentLength > maxBytes) { error = "toobig"; return null; }
                    using (Stream s = res.GetResponseStream())
                    {
                        MemoryStream ms = new MemoryStream();
                        byte[] buf = new byte[32768];
                        long total = 0;
                        int n;
                        while ((n = s.Read(buf, 0, buf.Length)) > 0)
                        {
                            total += n;
                            if (total > maxBytes) { error = "toobig"; return null; }
                            ms.Write(buf, 0, n);
                        }
                        byte[] png = NormalizePng(ms.ToArray());
                        if (png == null) { error = "notimage"; return null; }
                        if (!BigEnough(png)) { error = "tiny"; return null; }
                        return png;
                    }
                }
            }
            catch (WebException we)
            {
                error = we.Status == WebExceptionStatus.Timeout ? "timeout" : "neterr";
                return null;
            }
            catch (Exception ex)
            {
                Store.LogStartup("fetch: " + ex.GetType().Name);
                error = "neterr";
                return null;
            }
        }

        static byte[] NormalizePng(byte[] raw)
        {
            if (raw == null || raw.Length < 8) return null;
            if (raw[0] == 0x89 && raw[1] == 0x50 && raw[2] == 0x4E && raw[3] == 0x47) return raw;
            try
            {
                using (MemoryStream ms = new MemoryStream(raw))
                using (Image img = Image.FromStream(ms))
                    return PngOf(img);
            }
            catch { return null; }
        }

        // The BCL already declares FORMATETC/STGMEDIUM, so the COM calls use those types
        // directly instead of duplicating the layout.
        [DllImport("ole32.dll")]
        static extern int ReleaseStgMedium(
            ref System.Runtime.InteropServices.ComTypes.STGMEDIUM medium);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern uint RegisterClipboardFormatW(string name);

        static System.Runtime.InteropServices.ComTypes.FORMATETC MakeEtc(
            string format, int index, System.Runtime.InteropServices.ComTypes.TYMED tymed)
        {
            System.Runtime.InteropServices.ComTypes.FORMATETC fe =
                new System.Runtime.InteropServices.ComTypes.FORMATETC();
            fe.cfFormat = (short)RegisterClipboardFormatW(format);
            fe.ptd = IntPtr.Zero;
            fe.dwAspect = System.Runtime.InteropServices.ComTypes.DVASPECT.DVASPECT_CONTENT;
            fe.lindex = index;
            fe.tymed = tymed;
            return fe;
        }

        static void ReadVirtual(System.Runtime.InteropServices.ComTypes.IDataObject com, List<byte[]> sinks)
        {
            int count = 0;
            System.Runtime.InteropServices.ComTypes.FORMATETC fe = MakeEtc(
                "FileGroupDescriptorW", -1, System.Runtime.InteropServices.ComTypes.TYMED.TYMED_HGLOBAL);
            System.Runtime.InteropServices.ComTypes.STGMEDIUM stg =
                new System.Runtime.InteropServices.ComTypes.STGMEDIUM();
            // The managed COM wrapper throws instead of returning an HRESULT, so a source that
            // does not implement FileContents simply lands in the catch.
            try { com.GetData(ref fe, out stg); }
            catch { return; }
            try
            {
                if (stg.unionmember != IntPtr.Zero) count = Marshal.ReadInt32(stg.unionmember);
            }
            catch { count = 0; }
            finally { ReleaseStgMedium(ref stg); }
            if (count <= 0) return;
            if (count > 32) count = 32;
            for (int i = 0; i < count; i++)
            {
                byte[] b = VirtualBytes(com, i);
                if (b != null) sinks.Add(b);
            }
        }

        static byte[] VirtualBytes(System.Runtime.InteropServices.ComTypes.IDataObject com, int index)
        {
            System.Runtime.InteropServices.ComTypes.FORMATETC fe = MakeEtc(
                "FileContents", index, System.Runtime.InteropServices.ComTypes.TYMED.TYMED_ISTREAM);
            System.Runtime.InteropServices.ComTypes.STGMEDIUM stg =
                new System.Runtime.InteropServices.ComTypes.STGMEDIUM();
            try { com.GetData(ref fe, out stg); }
            catch { return null; }
            try
            {
                if (stg.unionmember == IntPtr.Zero) return null;
                object o = Marshal.GetObjectForIUnknown(stg.unionmember);
                System.Runtime.InteropServices.ComTypes.IStream s =
                    o as System.Runtime.InteropServices.ComTypes.IStream;
                if (s == null) return null;
                return NormalizePng(ReadStream(s, MaxImageBytes));
            }
            catch { return null; }
            finally { ReleaseStgMedium(ref stg); }
        }

        static byte[] ReadStream(System.Runtime.InteropServices.ComTypes.IStream s, int maxBytes)
        {
            byte[] buf = new byte[64 * 1024];
            MemoryStream ms = new MemoryStream();
            Stopwatch sw = Stopwatch.StartNew();
            while (true)
            {
                if (sw.ElapsedMilliseconds > ReadBudgetMs) return null;
                IntPtr got = Marshal.AllocHGlobal(4);
                int n;
                try { s.Read(buf, buf.Length, got); n = Marshal.ReadInt32(got); }
                catch { return null; }
                finally { Marshal.FreeHGlobal(got); }
                if (n <= 0) break;
                ms.Write(buf, 0, n);
                if (ms.Length > maxBytes) return null;
            }
            return ms.Length == 0 ? null : ms.ToArray();
        }
    }
    class Drawer : Form
    {
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.RoundRegion(this, 10f);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // CS_DROPSHADOW(0x20000)：让系统给无边框窗口画一圈投影。
                // 自绘阴影需要分层窗口（逐像素 alpha），那会牵动拖拽与滚动，这里不碰几何。
                cp.ClassStyle |= 0x20000;
                return cp;
            }
        }

        const int WM_NCHITTEST = 0x0084;
        const int HTCLIENT = 1;
        const int HTLEFT = 10;
        const int HTRIGHT = 11;
        const int HTTOP = 12;
        const int HTTOPLEFT = 13;
        const int HTTOPRIGHT = 14;
        const int HTBOTTOM = 15;
        const int HTBOTTOMLEFT = 16;
        const int HTBOTTOMRIGHT = 17;
        // Narrowest width at which the three bottom-bar entries still fit with a sliver of
        // hint text left over: measured by --barprobe in the worst case (500 ticked, 500 used).
        public const int MinW = 320;
        public const int MinH = 360;
        // Height of the grab strip along the top. It exists so the panel can be moved with the
        // mouse: the form has no title bar, so the strip is hit-tested as HTCAPTION.
        const int HeaderH = 30;
        const int HTCAPTION = 2;

        readonly QuoteListView _list = new QuoteListView();
        readonly IntPtr _target;
        GearBubble _gear;

        public Drawer(IntPtr target)
        {
            _target = target;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Theme.ToolBg;
            Padding = new Padding(1, 1 + HeaderH, 1, 1);
            KeyPreview = true;
            Text = "QuoteStack";
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint, true);
            _list.Dock = DockStyle.Fill;
            Controls.Add(_list);
            _list.CloseRequested += delegate { Close(); };
            _list.CleanupRequested += delegate
            {
                EventHandler h = CleanupRequested;
                if (h != null) h(this, EventArgs.Empty);
            };
            _list.DeleteRequested += delegate
            {
                EventHandler h = DeleteRequested;
                if (h != null) h(this, EventArgs.Empty);
            };
            _list.Reload();
            HookDrop(this);
            HookDrop(_list);
        }

        // A drop anywhere on the drawer counts, not just on the rows: the list only covers the
        // middle of the panel, and asking the user to aim at it would be needless precision.
        void HookDrop(Control c)
        {
            c.AllowDrop = true;
            c.DragEnter += DropEffect;
            c.DragOver += DropEffect;
            c.DragDrop += DropReceived;
        }

        static void DropEffect(object sender, DragEventArgs e)
        {
            e.Effect = DragIn.Supported(e.Data) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        void DropReceived(object sender, DragEventArgs e)
        {
            DragIn.Payload p = null;
            try { p = DragIn.Read(e.Data); }
            catch { p = null; }
            DragIn.Handler h = Dropped;
            if (h != null) h(p);
        }

        public event DragIn.Handler Dropped;

        public event EventHandler CleanupRequested;

        public event EventHandler DeleteRequested;

        public event EventHandler SettingsRequested;

        public IntPtr TargetWindow { get { return _target; } }

        public QuoteListView ListView { get { return _list; } }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Theme.RoundRegion(this, 10f);
            int w = Math.Max(MinW, Width);
            int h = Math.Max(MinH, Height);
            if (w != Width || h != Height) Size = new Size(w, h);
            SyncGear();
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            SyncGear();
        }

        // The gear is hidden with the panel, not closed: PerformInsert hides the drawer for the
        // paste and shows it again, and the button has to come back with it.
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (_gear == null) return;
            if (Visible) ShowGear();
            else _gear.Hide();
        }

        void ShowGear()
        {
            if (_gear == null)
            {
                _gear = new GearBubble();
                _gear.Clicked += delegate
                {
                    EventHandler h = SettingsRequested;
                    if (h != null) h(this, EventArgs.Empty);
                };
            }
            _gear.Show();
            SyncGear();
        }

        void SyncGear()
        {
            if (_gear == null || _gear.IsDisposed) return;
            // Outside the left edge by default; if the panel is hard against the left side of the
            // screen there is no room there, so it tucks just inside instead of hanging off-screen.
            int x = Left - _gear.Width - 6;
            int y = Top + (Height - _gear.Height) / 2;
            Rectangle wa = Screen.FromControl(this).WorkingArea;
            if (x < wa.Left) x = Left + 8;
            if (y < wa.Top) y = wa.Top;
            if (y + _gear.Height > wa.Bottom) y = wa.Bottom - _gear.Height;
            _gear.Location = new Point(x, y);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ShowGear();
            _list.Focus();
        }

        // The size is written once, on close, so dragging an edge does not hammer the disk.
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (WindowState == FormWindowState.Normal && Width >= MinW && Height >= MinH)
                Config.SaveDrawerSize(Width, Height);
            if (_gear != null)
            {
                _gear.Hide();
                _gear.Dispose();
                _gear = null;
            }
            base.OnFormClosing(e);
        }

        // The strip is the only chrome the panel has: it names the panel and is the area the
        // user drags. Painted here so the header scrolls with the form, never with the list.
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            using (var b = new SolidBrush(Theme.FieldBg))
                g.FillRectangle(b, 1, 1, ClientSize.Width - 2, HeaderH);
            using (var b = new SolidBrush(Theme.TextBody))
            using (var f = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold))
                g.DrawString("引用", f, b, 12f, 7f);
            using (var b = new SolidBrush(Theme.TextSoft))
            using (var f = new Font("Microsoft YaHei UI", 8.5f))
                g.DrawString("拖动这里移动 · 边缘可缩放 · Esc 关闭", f, b, 52f, 9f);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                Close();
            }
        }

        // The panel has no title bar, so resizing is done by hit-testing its edges.
        protected override void WndProc(ref Message m)
        {
            // 交互式缩放时系统会把旧尺寸的后备缓冲贴进新区域（表现为同一行被横向复制）。
            // 每次尺寸变化后强制整块重绘，重影即消失。
            if (m.Msg == 0x0005)
            {
                if (_list != null && !_list.IsDisposed) { _list.Invalidate(); _list.Update(); }
                Invalidate(); Invalidate(true);   // 标题条由窗体自己画，缩放后同样要整块失效
            }

            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);
                if (m.Result.ToInt32() == HTCLIENT)
                {
                    long lp = m.LParam.ToInt64();
                    Point p = PointToClient(new Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF)));
                    int gpx = 8;
                    bool left = p.X <= gpx;
                    bool right = p.X >= ClientSize.Width - gpx;
                    bool top = p.Y <= gpx;
                    bool bottom = p.Y >= ClientSize.Height - gpx;
                    if (left && top) m.Result = (IntPtr)HTTOPLEFT;
                    else if (right && top) m.Result = (IntPtr)HTTOPRIGHT;
                    else if (left && bottom) m.Result = (IntPtr)HTBOTTOMLEFT;
                    else if (right && bottom) m.Result = (IntPtr)HTBOTTOMRIGHT;
                    else if (left) m.Result = (IntPtr)HTLEFT;
                    else if (right) m.Result = (IntPtr)HTRIGHT;
                    else if (top) m.Result = (IntPtr)HTTOP;
                    else if (bottom) m.Result = (IntPtr)HTBOTTOM;
                    // No edge: the top strip works as the title bar so the panel can be moved.
                    else if (p.Y < HeaderH) m.Result = (IntPtr)HTCAPTION;
                }
                return;
            }
            base.WndProc(ref m);
        }
    }

    static class Native
    {
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        // Win11 原生窗口圆角：DWMWA_WINDOW_CORNER_PREFERENCE(33) = DWMWCP_ROUND(2)。
        // Win10 上调用无效但不报错，会保持方角。
        internal static int DwmRound(IntPtr h)
        {
            if (h == IntPtr.Zero) return -1;
            try { int v = 2; return DwmSetWindowAttribute(h, 33, ref v, 4); } catch { return -2; }
        }

        internal static void RoundWindow(IntPtr h)
        {
            if (h == IntPtr.Zero) return;
            try { int v = 2; DwmSetWindowAttribute(h, 33, ref v, 4); } catch { }
        }

        public const uint MOD_ALT = 0x0001;
        const int CF_UNICODETEXT = 13;
        const uint GMEM_MOVEABLE = 0x0002;
        const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct INPUTUNION
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public INPUTUNION u;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern uint GetClipboardSequenceNumber();

        [DllImport("user32.dll")]
        static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("user32.dll")]
        static extern int EnumClipboardFormats(uint format);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetClipboardFormatName(uint format, StringBuilder lpszFormatName, int cchMaxCount);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll")]
        static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        static extern IntPtr GlobalFree(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr h);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool QueryFullProcessImageName(IntPtr hProcess, uint flags, StringBuilder exeName, ref int size);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        public static string TitleOf(IntPtr hwnd)
        {
            var sb = new StringBuilder(512);
            GetWindowText(hwnd, sb, sb.Capacity);
            return sb.ToString();
        }

        public static string ProcessNameOf(IntPtr hwnd)
        {
            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            if (pid == 0) return "(unknown)";
            IntPtr h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (h == IntPtr.Zero) return "(denied)";
            try
            {
                var sb = new StringBuilder(1024);
                int size = sb.Capacity;
                if (!QueryFullProcessImageName(h, 0, sb, ref size)) return "(query-failed)";
                return Path.GetFileName(sb.ToString());
            }
            finally { CloseHandle(h); }
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool OpenProcessToken(IntPtr h, uint access, out IntPtr token);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool GetTokenInformation(IntPtr token, int cls, IntPtr info, int len, out int ret);

        const uint TOKEN_QUERY = 0x0008;
        const uint PROCESS_QUERY_INFORMATION = 0x0400;
        const int TokenIntegrityLevel = 25;

        public static int InputSize()
        {
            return Marshal.SizeOf(typeof(INPUT));
        }

        // Windows drops SendInput into a window whose process runs at a higher
        // integrity level (UIPI) and still reports the events as accepted, so the
        // level has to be read instead of assumed. The label is read straight from
        // the SID's last subauthority: 4096 low, 8192 medium, 12288 high.
        public static string IntegrityLabel(IntPtr hProcess)
        {
            IntPtr token;
            if (!OpenProcessToken(hProcess, TOKEN_QUERY, out token))
                return "token-denied(" + Marshal.GetLastWin32Error() + ")";
            IntPtr buf = IntPtr.Zero;
            try
            {
                int len;
                GetTokenInformation(token, TokenIntegrityLevel, IntPtr.Zero, 0, out len);
                if (len <= 0) return "info-failed(" + Marshal.GetLastWin32Error() + ")";
                buf = Marshal.AllocHGlobal(len);
                if (!GetTokenInformation(token, TokenIntegrityLevel, buf, len, out len))
                    return "info-failed(" + Marshal.GetLastWin32Error() + ")";
                IntPtr sid = Marshal.ReadIntPtr(buf);
                if (sid == IntPtr.Zero) return "no-sid";
                byte count = Marshal.ReadByte(sid, 1);
                if (count == 0) return "bad-sid";
                int off = 8 + (count - 1) * 4;
                int rid = Marshal.ReadInt32(sid, off);
                switch (rid)
                {
                    case 4096: return "low(4096)";
                    case 8192: return "medium(8192)";
                    case 12288: return "high(12288)";
                    case 16384: return "system(16384)";
                }
                return "rid(" + rid + ")";
            }
            finally
            {
                if (buf != IntPtr.Zero) Marshal.FreeHGlobal(buf);
                CloseHandle(token);
            }
        }

        public static string WindowIntegrity(IntPtr hwnd)
        {
            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            if (pid == 0) return "(no-pid)";
            IntPtr h = OpenProcess(PROCESS_QUERY_INFORMATION, false, pid);
            if (h == IntPtr.Zero) h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (h == IntPtr.Zero) return "process-denied(" + Marshal.GetLastWin32Error() + ")";
            try { return IntegrityLabel(h); }
            finally { CloseHandle(h); }
        }

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vk);

        // A hotkey fires on key-down, so at that instant the user's own Alt/Shift may
        // still be physically down. Injected keystrokes would then be read as
        // Alt+Shift+Ctrl+C, which is not a copy command at all. Wait for the user's
        // modifiers to clear before typing on their behalf.
        public static string HeldModifiers()
        {
            string s = "";
            if ((GetAsyncKeyState(0x12) & 0x8000) != 0) s += "ALT,";
            if ((GetAsyncKeyState(0x11) & 0x8000) != 0) s += "CTRL,";
            if ((GetAsyncKeyState(0x10) & 0x8000) != 0) s += "SHIFT,";
            if ((GetAsyncKeyState(0x5B) & 0x8000) != 0) s += "LWIN,";
            if ((GetAsyncKeyState(0x5C) & 0x8000) != 0) s += "RWIN,";
            return s.Length == 0 ? "-" : s.TrimEnd(',');
        }

        public static long WaitModifiersReleased(int maxMs)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < maxMs && !HeldModifiers().Equals("-"))
                Thread.Sleep(10);
            return sw.ElapsedMilliseconds;
        }

        static bool OpenClip(int tries)
        {
            for (int i = 0; i < tries; i++)
            {
                if (OpenClipboard(IntPtr.Zero)) return true;
                Thread.Sleep(20);
            }
            return false;
        }

        // What the clipboard held before we touched it. Text alone cannot restore an
        // image, so the backup carries PNG bytes as well; a null both means "leave ours".
        public class ClipBackup
        {
            public string Text;
            public byte[] Image;
        }

        public const int MaxImageBytes = 20 * 1024 * 1024;

        // Names of the formats currently on the clipboard, at most `max` of them. Diagnostics
        // only: a name such as "Bitmap"/"DIB"/"HTML Format" says whether the page copied a
        // picture at all, and nothing but the names is read (red line 8: no contents logged).
        public static string ClipFormatNames(int max)
        {
            if (!OpenClip(3)) return "locked";
            try
            {
                StringBuilder sb = new StringBuilder();
                int n = 0;
                uint f = 0;
                while (n < max)
                {
                    int next = EnumClipboardFormats(f);
                    if (next == 0) break;
                    f = (uint)next;
                    if (n > 0) sb.Append(',');
                    sb.Append(FormatName(f));
                    n++;
                }
                return n == 0 ? "empty" : sb.ToString();
            }
            catch { return "err"; }
            finally { CloseClipboard(); }
        }

        static string FormatName(uint f)
        {
            switch (f)
            {
                case 1: return "Text";
                case 2: return "Bitmap";
                case 3: return "MetafilePict";
                case 8: return "DIB";
                case 13: return "UnicodeText";
                case 15: return "HDROP";
                case 17: return "DIBV5";
            }
            if (f >= 0xC000)
            {
                StringBuilder name = new StringBuilder(160);
                int len = GetClipboardFormatName(f, name, 160);
                if (len > 0) return name.ToString(0, Math.Min(len, 40));
            }
            return "f" + f;
        }

        // Reads the clipboard picture as PNG bytes.
        //
        // System.Windows.Forms.Clipboard does NOT throw on a non-STA thread — it silently
        // reports "no image" (measured 2026-10-30: main-STA=True, MTA-thread=False,
        // pool-thread=False, STA-thread=True with the same clipboard contents). The capture
        // path runs on a ThreadPool worker, so an image copied in the browser was never seen
        // and every picture capture ended as "no selection" — the bug reported as
        // "引用不了图片". Reading on a short dedicated STA thread keeps the one tested OLE
        // path working from any thread.
        public static byte[] ReadClipImagePng(int maxBytes)
        {
            byte[] result = null;
            Thread t = new Thread(delegate()
            {
                try { result = ReadClipImagePngSta(maxBytes); }
                catch (Exception ex)
                {
                    // Logging itself can fail (read-only profile); an unhandled exception on a
                    // background thread would take the whole process down, so it is confined.
                    try { Store.LogStartup("image: read-failed " + ex.GetType().Name); } catch { }
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
            if (!t.Join(2000)) { try { Store.LogStartup("image: read-timeout"); } catch { } }
            return result;
        }

        static byte[] ReadClipImagePngSta(int maxBytes)
        {
            if (!Clipboard.ContainsImage()) return null;
            using (Image img = Clipboard.GetImage())
            {
                if (img == null) return null;
                using (var ms = new MemoryStream())
                {
                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    if (ms.Length > maxBytes) return null;
                    return ms.ToArray();
                }
            }
        }

        // Replaces the clipboard with one picture. OLE takes the clipboard lock itself, so
        // this must NOT run while our own OpenClip() handle is held: doing that made every
        // call fail (found by a headless round-trip test, 2026-10-30).
        public static bool SetClipboardImagePng(byte[] png)
        {
            if (png == null || png.Length == 0) return false;
            try
            {
                using (var ms = new MemoryStream(png))
                using (Image img = Image.FromStream(ms))
                {
                    Clipboard.SetImage(img);
                }
                return true;
            }
            catch { return false; }
        }

        public static bool ClipboardSnapshot(out ClipBackup backup, out int formats, out uint seq)
        {
            backup = new ClipBackup();
            formats = 0;
            seq = GetClipboardSequenceNumber();
            if (!OpenClip(5)) return false;
            try
            {
                uint f = 0;
                while ((f = (uint)EnumClipboardFormats(f)) != 0) formats++;

                if (IsClipboardFormatAvailable(CF_UNICODETEXT))
                {
                    IntPtr h = GetClipboardData(CF_UNICODETEXT);
                    if (h != IntPtr.Zero)
                    {
                        IntPtr p = GlobalLock(h);
                        if (p != IntPtr.Zero)
                        {
                            try { backup.Text = Marshal.PtrToStringUni(p); }
                            finally { GlobalUnlock(h); }
                        }
                    }
                }
            }
            finally { CloseClipboard(); }

            // The picture is read through the managed API only after our own handle is
            // released: holding the clipboard open while OLE asks for it would deadlock.
            backup.Image = ReadClipImagePng(MaxImageBytes);
            return true;
        }

        public static string ReadClipText(int tries)
        {
            for (int i = 0; i < tries; i++)
            {
                if (OpenClip(3))
                {
                    try
                    {
                        if (IsClipboardFormatAvailable(CF_UNICODETEXT))
                        {
                            IntPtr h = GetClipboardData(CF_UNICODETEXT);
                            if (h != IntPtr.Zero)
                            {
                                IntPtr p = GlobalLock(h);
                                if (p != IntPtr.Zero)
                                {
                                    try
                                    {
                                        string s = Marshal.PtrToStringUni(p);
                                        if (s != null) return s;
                                    }
                                    finally { GlobalUnlock(h); }
                                }
                            }
                        }
                    }
                    finally { CloseClipboard(); }
                }
                Thread.Sleep(25);
            }
            return null;
        }

        public static void ClipboardRestore(ClipBackup backup)
        {
            if (backup == null) return;
            if (backup.Image != null) { SetClipboardImagePng(backup.Image); return; }
            if (!OpenClip(5)) return;
            try
            {
                EmptyClipboard();
                if (backup.Text != null && backup.Text.Length > 0) SetTextInternal(backup.Text);
            }
            finally { CloseClipboard(); }
        }

        static bool SetTextInternal(string s)
        {
            int bytes = (s.Length + 1) * 2;
            IntPtr h = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)(uint)bytes);
            if (h == IntPtr.Zero) return false;
            IntPtr p = GlobalLock(h);
            if (p == IntPtr.Zero) { GlobalFree(h); return false; }
            try
            {
                char[] chars = s.ToCharArray();
                Marshal.Copy(chars, 0, p, chars.Length);
                Marshal.WriteInt16(p, s.Length * 2, 0);
            }
            finally { GlobalUnlock(h); }
            if (SetClipboardData(CF_UNICODETEXT, h) == IntPtr.Zero) { GlobalFree(h); return false; }
            return true;
        }

        // Replaces the clipboard with plain text. Only called for an explicit insert
        // action, never while capturing.
        public static bool SetClipboardText(string s)
        {
            if (!OpenClip(5)) return false;
            try
            {
                if (!EmptyClipboard()) return false;
                return SetTextInternal(s);
            }
            finally { CloseClipboard(); }
        }

        public static int SendCtrlV(int delayMs)
        {
            return SendCtrlChord(0x56, delayMs);
        }

        // Brings the remembered window back and confirms it really is foreground before
        // anything is typed. Verification is the point: SetForegroundWindow can silently
        // be refused by the foreground lock.
        public static bool EnsureForeground(IntPtr target, int maxMs)
        {
            if (target == IntPtr.Zero) return false;
            if (GetForegroundWindow() == target) return true;
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < maxMs)
            {
                SetForegroundWindow(target);
                Thread.Sleep(60);
                if (GetForegroundWindow() == target) return true;
            }
            return GetForegroundWindow() == target;
        }

        public static uint WaitClipboardChange(uint seq0, int timeoutMs)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                uint s = GetClipboardSequenceNumber();
                if (s != seq0) return s;
                // 8ms: a copy normally lands in well under 100ms, so the old 25ms poll was
                // adding up to 25ms of avoidable latency to every probe.
                Thread.Sleep(8);
            }
            return seq0;
        }

        static int SendOne(ushort vk, bool up)
        {
            INPUT[] a = new INPUT[1];
            a[0].type = 1;
            a[0].u.ki.wVk = vk;
            a[0].u.ki.wScan = 0;
            a[0].u.ki.dwFlags = up ? 2u : 0u;
            a[0].u.ki.time = 0;
            a[0].u.ki.dwExtraInfo = IntPtr.Zero;
            return (int)SendInput(1, a, Marshal.SizeOf(typeof(INPUT)));
        }

        public static int SendCtrlC(int delayMs)
        {
            return SendCtrlChord(0x43, delayMs);
        }

        // Ctrl+L focuses and selects the Edge address bar; the next Ctrl+C copies the URL.
        public static int SendCtrlL(int delayMs)
        {
            return SendCtrlChord(0x4C, delayMs);
        }

        // Returns focus from the address bar to the page, so the next capture does not
        // start with the URL bar already selected.
        public static int SendEscape(int delayMs)
        {
            int sent = 0;
            sent += SendOne(0x1B, false);
            if (delayMs > 0) Thread.Sleep(delayMs);
            sent += SendOne(0x1B, true);
            return sent;
        }

        public static uint Seq()
        {
            return GetClipboardSequenceNumber();
        }

        // Chromium treats Ctrl+Insert as "copy" as well. Used only as a second attempt
        // after Ctrl+C left the clipboard untouched, so the log can show which chord
        // the target actually honours.
        public static int SendCtrlInsert(int delayMs)
        {
            return SendCtrlChord(0x2D, delayMs);
        }

        public static int SendCtrlChord(ushort vk, int delayMs)
        {
            return SendChord(0x11, vk, delayMs);
        }

        // Alt+D is the Chromium alias for "focus the address bar"; used when Ctrl+L was
        // accepted by SendInput but the clipboard never changed.
        public static int SendAltD(int delayMs)
        {
            return SendChord(0x12, 0x44, delayMs);
        }

        public static int SendChord(ushort modVk, ushort vk, int delayMs)
        {
            int sent = 0;
            sent += SendOne(modVk, false);
            if (delayMs > 0) Thread.Sleep(delayMs);
            sent += SendOne(vk, false);
            if (delayMs > 0) Thread.Sleep(delayMs);
            sent += SendOne(vk, true);
            if (delayMs > 0) Thread.Sleep(delayMs);
            sent += SendOne(modVk, true);
            return sent;
        }

        // Releases modifiers that are still reported as physically down. A hotkey fires
        // on key-down, and if the user's own Alt/Shift is still held the injected chord
        // arrives as Alt+Shift+Ctrl+C, which no application reads as "copy".
        public static int ForceReleaseModifiers()
        {
            int n = 0;
            if ((GetAsyncKeyState(0x12) & 0x8000) != 0) n += SendOne(0x12, true);
            if ((GetAsyncKeyState(0x11) & 0x8000) != 0) n += SendOne(0x11, true);
            if ((GetAsyncKeyState(0x10) & 0x8000) != 0) n += SendOne(0x10, true);
            if ((GetAsyncKeyState(0x5B) & 0x8000) != 0) n += SendOne(0x5B, true);
            if ((GetAsyncKeyState(0x5C) & 0x8000) != 0) n += SendOne(0x5C, true);
            return n;
        }
    }
}
