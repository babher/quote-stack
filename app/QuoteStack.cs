// QuoteStack P3 skeleton: always-on-top floating bar + Alt+Q capture from Edge.
// Single file, .NET Framework WinForms, no third-party dependencies.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace QuoteStack
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                // Test-only hook: redirect storage so the self-test can run inside a
                // sandboxed directory. Normal runs never pass --data.
                if (args[i] == "--data" && i + 1 < args.Length)
                {
                    Store.DataDirOverride = args[i + 1];
                    i++;
                }
            }
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
                if (a == "--selftest")
                {
                    Environment.Exit(Store.SelfTest());
                    return;
                }
                // Test-only hook: writes the composed insert text for two fixed records to
                // the given path so the formatting can be compared byte for byte. Uses a
                // fixed "now" so the expected output does not drift with the clock.
                if (a == "--compose" && Array.IndexOf(args, a) + 1 < args.Length)
                {
                    List<Record> recs = new List<Record>();
                    Record r1 = new Record();
                    r1.Text = "AAA";
                    r1.Source = "Edge";
                    r1.Url = "https://example.com/a";
                    r1.When = new DateTimeOffset(2026, 10, 28, 13, 44, 0, TimeSpan.FromHours(8));
                    Record r2 = new Record();
                    r2.Text = "BBB";
                    r2.Source = "Word";
                    r2.When = new DateTimeOffset(2025, 12, 31, 9, 5, 0, TimeSpan.FromHours(8));
                    recs.Add(r1);
                    recs.Add(r2);
                    string body = Records.Compose(recs, new DateTimeOffset(2026, 10, 28, 14, 0, 0, TimeSpan.FromHours(8)));
                    File.WriteAllText(args[Array.IndexOf(args, a) + 1], body, new UTF8Encoding(false));
                    Environment.Exit(0);
                    return;
                }
                // Test-only hook: dumps what the drawer would show (group, stamp, source,
                // length) so the grouping can be checked without clicking the UI.
                if (a == "--list" && Array.IndexOf(args, a) + 1 < args.Length)
                {
                    List<Record> recs = Records.Load(200);
                    StringBuilder sb = new StringBuilder();
                    sb.Append("count=").Append(recs.Count).Append("\n");
                    DateTimeOffset now = DateTimeOffset.Now;
                    foreach (Record r in recs)
                    {
                        sb.Append(Records.GroupOf(r, now)).Append(" | ")
                          .Append(Records.Stamp(r, now)).Append(" | ")
                          .Append(Records.SrcLabel(r)).Append(" | len=").Append(r.Text.Length).Append("\n");
                    }
                    File.WriteAllText(args[Array.IndexOf(args, a) + 1], sb.ToString(), new UTF8Encoding(false));
                    Environment.Exit(0);
                    return;
                }
                // Test-only hook: shows which strings the URL filter accepts, so a wrong
                // acceptance (placeholder text, file:// path) is visible instead of assumed.
                if (a == "--urlcheck" && Array.IndexOf(args, a) + 1 < args.Length)
                {
                    string[] samples = new string[]
                    {
                        "https://example.com/a?b=1",
                        "  http://x.y/z  ",
                        "edge://settings",
                        "搜索或输入网址",
                        "https://example.com/a b",
                        "file:///C:/x.html",
                        "https://",
                        "https://example.com/" + new string('a', 2100)
                    };
                    StringBuilder sb = new StringBuilder();
                    for (int k = 0; k < samples.Length; k++)
                        sb.Append(k).Append(" | len=").Append(samples[k].Length)
                          .Append(" | ").Append(Capture.LooksLikeUrl(samples[k]) ? "ok" : "reject").Append("\n");
                    File.WriteAllText(args[Array.IndexOf(args, a) + 1], sb.ToString(), new UTF8Encoding(false));
                    Environment.Exit(0);
                    return;
                }
                // Test-only hook: ticks rows out of order and prints what Checked() returns,
                // so the "insert in the order the user ticked" rule can be checked headlessly.
                if (a == "--ordertest" && Array.IndexOf(args, a) + 1 < args.Length)
                {
                    string outPath = args[Array.IndexOf(args, a) + 1];
                    StringBuilder sb = new StringBuilder();
                    using (QuoteListView lv = new QuoteListView())
                    {
                        lv.Reload();
                        sb.Append("loaded=").Append(lv.Count).Append("\n");
                        foreach (int i in new int[] { 2, 0, 3 }) lv.Toggle(i);
                        sb.Append("picks=");
                        foreach (Record r in lv.Checked()) sb.Append(r.Text).Append(",");
                        sb.Append("\n");
                        lv.Toggle(0);   // un-ticking must drop it from the order, not just clear a flag
                        sb.Append("after-untick=");
                        foreach (Record r in lv.Checked()) sb.Append(r.Text).Append(",");
                        sb.Append("\n");
                        lv.Toggle(0);   // re-ticking puts it last
                        sb.Append("after-retick=");
                        foreach (Record r in lv.Checked()) sb.Append(r.Text).Append(",");
                        sb.Append("\n");
                        sb.Append("ordinals=");
                        foreach (Record r in lv.Checked()) sb.Append(r.Text).Append(":").Append(lv.OrdinalOf(r)).Append(" ");
                        sb.Append("\n");
                        sb.Append("ordinal-of-unticked=").Append(lv.OrdinalOf(lv.Sample(1))).Append("\n");
                    }
                    File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(false));
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

        public static string DataDirOverride;

        public static string DirPath
        {
            get
            {
                if (DataDirOverride != null) return DataDirOverride;
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

        public static bool Append(string text, string source, string title, string url, out string error)
        {
            error = null;
            var sb = new StringBuilder(256 + text.Length);
            sb.Append("{\"ts\":\"").Append(DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz")).Append("\",");
            sb.Append("\"source\":\"").Append(Escape(source)).Append("\",");
            sb.Append("\"title\":\"").Append(Escape(title)).Append("\",");
            sb.Append("\"url\":\"").Append(Escape(url == null ? "" : url)).Append("\",");
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
                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.GetType().Name + ": " + ex.Message;
                    return false;
                }
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

        // Writes one fixed record into the production file, then exits.
        // Used only by manual verification; it is tagged source=selftest so it is
        // distinguishable from real captures.
        public static int SelfTest()
        {
            string err;
            bool ok = Append("SELFTEST-RECORD 这条记录由 --selftest 写入，用于验证存储路径与追加写入。", "selftest", "selftest", "", out err);
            return ok ? 0 : 1;
        }

        static string Escape(string s)
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

        // Atomic replace: temp file first, then swap, so a crash cannot leave a
        // half-written config behind.
        public static void WriteText(string content)
        {
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, content, new UTF8Encoding(false));
            if (File.Exists(FilePath)) File.Replace(tmp, FilePath, null);
            else File.Move(tmp, FilePath);
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
            if (!string.Equals(proc, "msedge.exe", StringComparison.OrdinalIgnoreCase))
            {
                Store.LogStartup("capture-refused: fg=" + proc + " reason=not-edge");
                return new CaptureResult { Ok = false, HadSelection = true, Message = "仅支持 Edge" };
            }

            string title = Native.TitleOf(hwnd);

            string backup;
            int formats;
            uint seq0;
            bool hadSnapshot = Native.ClipboardSnapshot(out backup, out formats, out seq0);
            if (!hadSnapshot)
            {
                Store.LogStartup("capture-refused: fg=" + proc + " reason=clipboard-busy");
                return new CaptureResult { Ok = false, HadSelection = true, Message = "剪贴板被占用" };
            }

            Stopwatch sw = Stopwatch.StartNew();
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
            if (!string.Equals(procNow, "msedge.exe", StringComparison.OrdinalIgnoreCase))
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

            // The address bar is read only after the selection has been copied: Ctrl+L moves
            // focus to the URL bar, so anything sent afterwards would copy the URL instead of
            // the page text. A URL is stored only when it really looks like one.
            string url = null;
            int sentL = 0;
            int urlRawLen = -1;
            string urlStep = "skip";
            if (text != null && text.Trim().Length > 0)
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

            // Counts and integrity labels only: no captured text, no page title (red line 8).
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
                + " backupLen=" + (backup == null ? -1 : backup.Length)
                + " urlStep=" + urlStep
                + " urlRawLen=" + urlRawLen
                + " urlLen=" + (url == null ? -1 : url.Length)
                + " textLen=" + (text == null ? -1 : text.Length));

            // No clipboard change means no selection: the same hotkey that stores a quote
            // must then open the drawer and insert at the caret instead (user rule m01388).
            if (text == null || text.Trim().Length == 0)
                return new CaptureResult { Ok = false, HadSelection = false, Message = null };

            string err;
            if (!Store.Append(text, proc, title, url, out err))
                return new CaptureResult { Ok = false, HadSelection = true, Message = "写入失败" };

            return new CaptureResult { Ok = true, HadSelection = true, Preview = text };
        }
    }

    class FloatBar : Form
    {
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

        int _count;
        string _preview;
        string _status;
        string _hint;
        string _cfgPath;
        int _flashKind;
        bool _busy;
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
            BackColor = Color.FromArgb(28, 28, 30);
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
                Store.LogStartup("hotkey-register-failed: " + wanted + " (config: " + _cfgPath + ")");
                MessageBox.Show(
                    "热键 " + wanted + " 注册失败，可能已被其他程序占用。\n\n" +
                    "请把配置文件里的 hotkey 改成别的组合（例如 Alt+Shift+Q），保存后重新启动：\n" + _cfgPath,
                    "QuoteStack",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                Environment.Exit(2);
            }

            _hint = "热键 " + wanted;
            Store.LogStartup("hotkey-registered: " + wanted);
            _hintTimer.Start();
            Invalidate();
        }

        void PlaceBottomRight()
        {
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(wa.Right - Width - 12, wa.Bottom - Height - 12);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                // One key, decided by what the user is doing (rule m01388):
                //   drawer focused  -> insert the ticked quotes into the last foreign window
                //   Edge foreground -> a selection means a quote, no selection means insert
                //   anything else   -> no way to tell, so insert into that window
                bool drawerUp = _drawer != null && !_drawer.IsDisposed && _drawer.Visible;
                if (drawerUp && _drawer.ContainsFocus) { PerformInsert(); return; }
                if (IsEdgeForeground()) { StartCapture(); return; }
                if (drawerUp) { PerformInsert(); return; }
                OpenOrFocusDrawer();
                return;
            }
            base.WndProc(ref m);
        }

        static bool IsEdgeForeground()
        {
            return string.Equals(Native.ProcessNameOf(Native.GetForegroundWindow()),
                "msedge.exe", StringComparison.OrdinalIgnoreCase);
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

        void Apply(CaptureResult r)
        {
            _busy = false;
            if (r.Ok)
            {
                _count = Store.Count();
                if (_count < 0) _count = 0;
                _preview = r.Preview;
                _status = null;
                _flashKind = KindOk;
                _flash.Interval = 2000;
                _plusAt = Environment.TickCount;
                _anim.Start();
            }
            else if (!r.HadSelection)
            {
                // No selection found: the same key means "insert here", so the drawer opens
                // on the window that was foreground a moment ago.
                _status = null;
                _flashKind = KindNone;
                _flash.Stop();
                Invalidate();
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
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            int w = 380;
            int h = Math.Min(520, wa.Height - 96);
            if (h < 320) h = 320;
            _drawer.Size = new Size(w, h);
            _drawer.Location = new Point(wa.Right - 16 - w, wa.Top + (wa.Height - h) / 2);
            _drawer.Show();
            _drawer.BringToFront();
            _drawer.Activate();
            _drawer.ListView.Focus();
        }

        void PerformInsert()
        {
            if (_drawer == null || _drawer.IsDisposed) return;
            List<Record> chosen = _drawer.ListView.Checked();
            if (chosen.Count == 0)
            {
                // Nothing ticked: nothing is inserted. The ticks are cleared after an insert
                // on purpose, so a second Enter must not paste the same quote again.
                _drawer.ListView.SetHint("没有勾选内容 · 先勾选再按 Enter");
                return;
            }
            string text = Records.Compose(chosen, DateTimeOffset.Now);
            // Where the caret actually is now, not where it was when the drawer opened.
            IntPtr target = _lastForeign;
            if (target == IntPtr.Zero) target = _drawer.TargetWindow;
            // Hidden rather than closed: it has to give the target back its focus for the
            // paste, then returns so the next quote can be inserted in the same pass.
            _drawer.Hide();

            string message;
            string log;
            bool ok = Deliver.Run(target, text, out message, out log);
            Store.LogStartup(log); // counts and outcomes only, never the text

            if (ok)
            {
                _count = Math.Max(0, Store.Count());
                _preview = chosen[0].Text;
                _status = "已插入 " + chosen.Count + " 条";
                _flashKind = KindOk;
                // Ticked off the list on purpose: a second Enter would otherwise insert
                // the same quote twice.
                _drawer.ListView.Uncheck(chosen);
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

            Color bg = Color.FromArgb(28, 28, 30);
            if (_flashKind == KindOk) bg = Color.FromArgb(38, 52, 92);
            else if (_flashKind == KindErr) bg = Color.FromArgb(72, 34, 34);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Rounded(rect, 12))
            {
                using (var b = new SolidBrush(bg)) g.FillPath(b, path);
                using (var pen = new Pen(Color.FromArgb(46, 255, 255, 255))) g.DrawPath(pen, path);
            }

            string cnt = _count.ToString() + " 条";
            SizeF cntSize = g.MeasureString(cnt, _fCount);
            using (var b = new SolidBrush(Color.FromArgb(245, 245, 247)))
                g.DrawString(cnt, _fCount, b, 14f, (Height - cntSize.Height) / 2f);

            float x = 14f + cntSize.Width + 10f;
            float avail = Width - x - 12f;

            string sub;
            Color subColor;
            if (_status != null)
            {
                sub = _status;
                subColor = Color.FromArgb(240, 170, 110);
            }
            else if (_hint != null)
            {
                sub = _hint;
                subColor = Color.FromArgb(120, 160, 255);
            }
            else
            {
                sub = _preview == null ? "还没有采集" : Flatten(_preview);
                subColor = Color.FromArgb(168, 168, 176);
            }
            if (avail <= 8f) return;

            while (sub.Length > 0 && g.MeasureString(sub, _fSub).Width > avail)
                sub = sub.Substring(0, sub.Length - 1);

            if (sub.Length > 0)
            {
                SizeF subSize = g.MeasureString(sub, _fSub);
                using (var b = new SolidBrush(subColor))
                    g.DrawString(sub, _fSub, b, x, (Height - subSize.Height) / 2f);
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
                    float dy = -10f * t;
                    if (alpha > 0)
                    {
                        SizeF ps = g.MeasureString("+1", _fPlus);
                        using (var b = new SolidBrush(Color.FromArgb(alpha, 106, 227, 160)))
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
        public DateTimeOffset When;
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
                    if (text == null || text.Trim().Length == 0) continue;
                    Record r = new Record();
                    r.Text = text;
                    r.Source = AppLabel(Store.JsonField(lines[i], "source"));
                    r.Url = Store.JsonField(lines[i], "url");
                    r.When = ParseWhen(Store.JsonField(lines[i], "ts"));
                    list.Add(r);
                }
            }
            catch { }
            return list;
        }

        static string AppLabel(string proc)
        {
            if (proc == null || proc.Length == 0) return "?";
            if (string.Equals(proc, "msedge.exe", StringComparison.OrdinalIgnoreCase)) return "Edge";
            if (string.Equals(proc, "WINWORD.EXE", StringComparison.OrdinalIgnoreCase)) return "Word";
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

            string backup;
            int formats;
            uint seq;
            Native.ClipboardSnapshot(out backup, out formats, out seq);
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
            Native.ClipboardRestore(backup);

            log = "deliver: sent=" + sent + " target=" + Native.ProcessNameOf(target)
                + " fgAfterSend=" + fgAfter
                + " waitedMods=" + waitedMods + " released=" + released
                + " textLen=" + text.Length + " restored=true costMs=" + sw.ElapsedMilliseconds;
            return true;
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

        public QuoteListView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint | ControlStyles.Selectable, true);
            BackColor = Color.FromArgb(24, 24, 26);
            TabStop = true;
        }

        public void Reload()
        {
            _items = Records.Load(200);
            _open = new bool[_items.Count];
            _checked = new bool[_items.Count]; // nothing is checked until the user says so
            _picks.Clear();
            _sel = _items.Count > 0 ? 0 : -1;
            _scroll = 0;
            _hint = null;
            Invalidate();
        }

        public int Count { get { return _items.Count; } }

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

        float RowBodyH(Graphics g, int i)
        {
            if (!_open[i]) return BodyLineH * 2;
            SizeF sz = g.MeasureString(_items[i].Text.Trim(), _fBody, new SizeF(BodyWidth, 0f));
            return Math.Max(BodyLineH * 2, sz.Height);
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

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            RebuildLayout(g);
            ClampScroll();

            int ch = ContentHeight;
            int n = _items.Count;
            DateTimeOffset now = DateTimeOffset.Now;

            if (n == 0)
            {
                using (var b = new SolidBrush(Color.FromArgb(150, 156, 170)))
                    g.DrawString("还没有记录。选中文字后按热键。", _fBody, b, PadLeft, 40f);
            }

            for (int gi = 0; gi < _groupNames.Count; gi++)
            {
                float gy = _groupY[gi] - _scroll;
                if (gy + GroupHeight < 0 || gy > ch) continue;
                using (var b = new SolidBrush(Color.FromArgb(126, 132, 146)))
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
                    using (var b = new SolidBrush(selected ? Color.FromArgb(44, 46, 54) : Color.FromArgb(30, 30, 34)))
                        g.FillPath(b, path);
                }

                string body = _open[i]
                    ? _items[i].Text.Trim()
                    : TwoLines(g, _items[i].Text.Trim(), _fBody, BodyWidth);
                using (var b = new SolidBrush(Color.FromArgb(232, 232, 238)))
                    g.DrawString(body, _fBody, b, new RectangleF(PadLeft, y + BodyTop, BodyWidth, _bodyH[i]));

                float metaY = y + BodyTop + _bodyH[i] + 4;
                string meta = _items[i].Source + " · " + Records.Stamp(_items[i], now);
                using (var b = new SolidBrush(Color.FromArgb(150, 156, 170)))
                    g.DrawString(meta, _fMeta, b, PadLeft, metaY);

                // Expanded rows also show where the quote came from, and a click copies it.
                if (_open[i] && HasUrl(i))
                {
                    float urlY = metaY + MetaH;
                    int used;
                    bool cut;
                    string shown = FitPrefix(g, _fMeta, _items[i].Url, BodyWidth, out used, out cut);
                    if (cut) shown = shown + "…";
                    using (var b = new SolidBrush(Color.FromArgb(96, 148, 255)))
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
                        using (var b = new SolidBrush(Color.FromArgb(106, 227, 160)))
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
                    using (var b = new SolidBrush(Color.FromArgb(58, 60, 68)))
                        g.FillRectangle(b, Width - 7, thumbY, 4, thumbH);
                }
            }

            // bottom bar
            int barY = Height - BottomBarH;
            using (var b = new SolidBrush(Color.FromArgb(34, 34, 38)))
                g.FillRectangle(b, 0, barY, Width, BottomBarH);
            int cnt = 0;
            for (int i = 0; i < n; i++) if (_checked[i]) cnt++;
            string bar = _hint != null
                ? _hint
                : (cnt > 0
                    ? ("已勾选 " + cnt + "/" + n + " · 按热键插入")
                    : "未勾选 · 勾选后按热键插入");
            Color barColor = _hint != null ? Color.FromArgb(240, 170, 110) : Color.FromArgb(140, 146, 160);
            using (var b = new SolidBrush(barColor))
                g.DrawString(bar, _fBar, b, PadLeft, barY + 9f);
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
                using (var pen = new Pen(Color.FromArgb(120, 126, 140), 1f))
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
            if (e.Y > ContentHeight) return;
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
                    _hint = "插入请按热键（Alt+Shift+Q）";
                    Invalidate();
                    break;
            }
        }
    }

    class Drawer : Form
    {
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
        const int MinW = 380;
        const int MinH = 320;

        readonly QuoteListView _list = new QuoteListView();
        readonly IntPtr _target;

        public Drawer(IntPtr target)
        {
            _target = target;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.FromArgb(52, 54, 62);
            Padding = new Padding(1);
            KeyPreview = true;
            Text = "QuoteStack";
            _list.Dock = DockStyle.Fill;
            Controls.Add(_list);
            _list.CloseRequested += delegate { Close(); };
            _list.Reload();
        }

        public IntPtr TargetWindow { get { return _target; } }

        public QuoteListView ListView { get { return _list; } }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            int w = Math.Max(MinW, Width);
            int h = Math.Max(MinH, Height);
            if (w != Width || h != Height) Size = new Size(w, h);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _list.Focus();
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
                }
                return;
            }
            base.WndProc(ref m);
        }
    }

    static class Native
    {
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

        public static bool ClipboardSnapshot(out string text, out int formats, out uint seq)
        {
            text = null;
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
                            try { text = Marshal.PtrToStringUni(p); }
                            finally { GlobalUnlock(h); }
                        }
                    }
                }
                return true;
            }
            finally { CloseClipboard(); }
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

        public static void ClipboardRestore(string backup)
        {
            if (!OpenClip(5)) return;
            try
            {
                EmptyClipboard();
                if (backup != null && backup.Length > 0) SetTextInternal(backup);
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
