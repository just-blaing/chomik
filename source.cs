// в этой программе используются иконки с сайта https://www.flaticon.com
// также все анимации были скачаны с сайта https://mv.darkok.xyz/anims.7z
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using Windows.Media.Control;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

public static class Program
{
  [STAThread]
  static void Main()
  {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run(new HamsterViewer());
  }
}
public class AnimationFrame
{
  public Bitmap Image { get; set; }
  public int Duration { get; set; }
  public AnimationFrame(Bitmap image, int duration)
  {
      Image = image;
      Duration = duration;
  }
}
public class CustomMessageBox : Form
{
  private Label messageLabel;
  private Button okButton;
  private int cornerRadius = 12;
  private Color borderColor = Color.FromArgb(148, 0, 211);
  private Color backgroundColor = Color.Black;
  private Color textColor = Color.White;
  private Color buttonBackColor = Color.FromArgb(40, 40, 40);
  private Color buttonForeColor = Color.White;
  private System.Windows.Forms.Timer? animationTimer = null;
  private List<AnimationFrame>? animationFrames = null;
  private int currentFrameIndex = 0;
  private Bitmap? currentFrameBitmap = null;
  private Panel buttonPanel;
  private Point mouseOffset;
  private bool isMouseDown = false;
  private const int WS_EX_TOOLWINDOW = 0x80;
  private const int WS_EX_TOPMOST = 0x00000008;
  public CustomMessageBox(string message, List<AnimationFrame>? animFrames = null)
  {
      FormBorderStyle = FormBorderStyle.None;
      StartPosition = FormStartPosition.CenterParent;
      BackColor = Color.Black;
      ShowInTaskbar = false;
      Size = new Size(300, 150);
      TopMost = true;
      DoubleBuffered = true;
      this.animationFrames = animFrames;
      if (this.animationFrames != null && this.animationFrames.Count > 0)
      {
          currentFrameIndex = 0;
          currentFrameBitmap = this.animationFrames[currentFrameIndex].Image;

          animationTimer = new System.Windows.Forms.Timer();
          animationTimer.Interval = this.animationFrames[currentFrameIndex].Duration > 0 ? this.animationFrames[currentFrameIndex].Duration : 100;
          animationTimer.Tick += AnimationTimer_Tick;
          animationTimer.Start();
      }
      messageLabel = new Label
      {
          Text = message,
          ForeColor = textColor,
          BackColor = Color.Transparent,
          TextAlign = ContentAlignment.MiddleCenter,
          Dock = DockStyle.Fill,
          Padding = new Padding(15),
          Font = new Font("Arial", 10)
      };
      messageLabel.MouseDown += Form_MouseDown;
      messageLabel.MouseMove += Form_MouseMove;
      messageLabel.MouseUp += Form_MouseUp;
      okButton = new Button
      {
          Text = "OK",
          DialogResult = DialogResult.OK,
          Size = new Size(80, 30),
          BackColor = buttonBackColor,
          ForeColor = buttonForeColor,
          FlatStyle = FlatStyle.Flat
      };
      okButton.FlatAppearance.BorderSize = 0;
      okButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, 60, 60);
      okButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 50);
      okButton.Click += (s, e) => Close();
      buttonPanel = new Panel
      {
          Dock = DockStyle.Bottom,
          Height = 40,
          BackColor = backgroundColor
      };
      buttonPanel.Controls.Add(okButton);
      buttonPanel.ControlAdded += (s, e) =>
      {
          if (e.Control is Button btn)
          {
              btn.Location = new Point((buttonPanel.Width - btn.Width) / 2, (buttonPanel.Height - btn.Height) / 2);
          }
      };
      buttonPanel.Resize += (s, e) =>
      {
          if (buttonPanel.Controls.Count > 0 && buttonPanel.Controls[0] is Button btn)
          {
              btn.Location = new Point((buttonPanel.Width - btn.Width) / 2, (buttonPanel.Height - btn.Height) / 2);
          }
      };
      Controls.Add(messageLabel);
      Controls.Add(buttonPanel);
      Padding = new Padding(1);
  }
  private void AnimationTimer_Tick(object? sender, EventArgs e)
  {
      if (animationFrames == null || animationFrames.Count == 0)
      {
          animationTimer?.Stop();
          return;
      }
      currentFrameIndex++;
      if (currentFrameIndex >= animationFrames.Count)
      {
          currentFrameIndex = 0;
      }
      try
      {
          currentFrameBitmap = animationFrames[currentFrameIndex].Image;
          int interval = animationFrames[currentFrameIndex].Duration;
          if (animationTimer != null)
          {
              animationTimer.Interval = interval > 0 ? interval : 100;
          }
          this.Invalidate();
      }
      catch (ObjectDisposedException)
      {
          animationTimer?.Stop();
      }
      catch (Exception ex)
      {
          Debug.WriteLine($"{ex.Message}");
          animationTimer?.Stop();
      }
  }
  protected override CreateParams CreateParams
  {
      get
      {
          CreateParams cp = base.CreateParams;
          cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
          return cp;
      }
  }
  protected override void OnPaint(PaintEventArgs e)
  {
      e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
      Rectangle bounds = new Rectangle(0, 0, Width, Height);
      using (GraphicsPath path = RoundedRect(bounds, cornerRadius))
      using (SolidBrush backgroundBrush = new SolidBrush(backgroundColor))
      using (Pen borderPen = new Pen(borderColor, 2))
      {
          e.Graphics.FillPath(backgroundBrush, path);
          if (currentFrameBitmap != null)
          {
              GraphicsState state = e.Graphics.Save();
              e.Graphics.SetClip(path);
              Rectangle animationDrawArea = new Rectangle(0, 0, Width, Height - buttonPanel.Height);
              if (animationDrawArea.Width > 0 && animationDrawArea.Height > 0)
              {
                  e.Graphics.DrawImage(currentFrameBitmap, animationDrawArea);
              }
              e.Graphics.Restore(state);
          }
          e.Graphics.DrawPath(borderPen, path);
      }
      base.OnPaint(e);
  }
  private GraphicsPath RoundedRect(Rectangle bounds, int radius)
  {
      int d = radius * 2;
      GraphicsPath path = new GraphicsPath();
      path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
      path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
      path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
      path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
      path.CloseFigure();
      return path;
  }
  protected override void OnFormClosing(FormClosingEventArgs e)
  {
      base.OnFormClosing(e);
      animationTimer?.Stop();
      animationTimer?.Dispose();
  }
  private void Form_MouseDown(object? sender, MouseEventArgs e)
  {
      if (e.Button == MouseButtons.Left)
      {
          mouseOffset = new Point(-e.X, -e.Y);
          isMouseDown = true;
      }
  }
  private void Form_MouseMove(object? sender, MouseEventArgs e)
  {
      if (isMouseDown)
      {
          Point mousePos = Control.MousePosition;
          mousePos.Offset(mouseOffset.X, mouseOffset.Y);
          Location = mousePos;
      }
  }
  private void Form_MouseUp(object? sender, MouseEventArgs e)
  {
      if (e.Button == MouseButtons.Left)
      {
          isMouseDown = false;
      }
  }
}
public class AnimatedMenuRenderer : ToolStripProfessionalRenderer
{
  private Dictionary<ToolStripItem, float> itemHighlightProgress = new Dictionary<ToolStripItem, float>();
  private System.Windows.Forms.Timer animationTimer;
  private int cornerRadius = 8;
  public HashSet<ToolStripItem> ItemsToExcludeFromHighlight { get; } = new HashSet<ToolStripItem>();
  public AnimatedMenuRenderer()
  {
      animationTimer = new System.Windows.Forms.Timer();
      animationTimer.Interval = 15;
      animationTimer.Tick += (s, e) =>
      {
          bool needRefresh = false;
          var itemsToUpdate = itemHighlightProgress.Keys.ToList();
          foreach (var item in itemsToUpdate)
          {
              if (item.Owner == null)
              {
                  itemHighlightProgress.Remove(item);
                  needRefresh = true;
                  continue;
              }
              if (item.Selected && !ItemsToExcludeFromHighlight.Contains(item))
              {
                  if (itemHighlightProgress.ContainsKey(item) && itemHighlightProgress[item] < 1f)
                  {
                      itemHighlightProgress[item] += 0.08f;
                      if (itemHighlightProgress[item] > 1f)
                          itemHighlightProgress[item] = 1f;
                      needRefresh = true;
                  }
                  else if (!itemHighlightProgress.ContainsKey(item))
                  {
                      itemHighlightProgress[item] = 0.08f;
                      needRefresh = true;
                  }
              }
              else
              {
                  if (itemHighlightProgress.ContainsKey(item) && itemHighlightProgress[item] > 0f)
                  {
                      itemHighlightProgress[item] -= 0.08f;
                      if (itemHighlightProgress[item] < 0f)
                          itemHighlightProgress[item] = 0f;
                      needRefresh = true;
                  }
                  else if (itemHighlightProgress.ContainsKey(item) && itemHighlightProgress[item] == 0f)
                  {
                      itemHighlightProgress.Remove(item);
                      needRefresh = true;
                  }
              }
          }
          if (needRefresh)
          {
              if (itemHighlightProgress.Keys.Any())
              {
                  itemHighlightProgress.Keys.First().Owner?.Invalidate();
              }
              else if (itemsToUpdate.Any() && itemsToUpdate.First().Owner != null)
              {
                  itemsToUpdate.First().Owner.Invalidate();
              }
          }
      };
      animationTimer.Start();
  }
  protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
  {
      using (SolidBrush brush = new SolidBrush(Color.Black))
      {
          e.Graphics.FillRectangle(brush, e.AffectedBounds);
      }
      Rectangle bounds = new Rectangle(Point.Empty, e.ToolStrip.Size);
      using (GraphicsPath path = RoundedRect(bounds, cornerRadius))
      using (SolidBrush brush = new SolidBrush(Color.Black))
      {
          e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
          e.Graphics.FillPath(brush, path);
      }
  }
  protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
  {
      Rectangle rc = new Rectangle(Point.Empty, e.Item.Size);
      int radius = 8;
      using (GraphicsPath path = RoundedRect(rc, radius))
      using (SolidBrush itemBackgroundBrush = new SolidBrush(Color.Black))
      {
          e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
          e.Graphics.FillPath(itemBackgroundBrush, path);
      }
      if (ItemsToExcludeFromHighlight.Contains(e.Item))
      {
          return;
      }
      float progress = 0f;
      if (itemHighlightProgress.ContainsKey(e.Item))
          progress = itemHighlightProgress[e.Item];
      if (progress > 0f)
      {
          Color targetColor = Color.FromArgb(173, 216, 230);
          Color blendColor = Color.FromArgb(
              (int)(progress * 150),
              targetColor.R,
              targetColor.G,
              targetColor.B
          );
          using (GraphicsPath path = RoundedRect(rc, radius))
          using (SolidBrush highlightBrush = new SolidBrush(blendColor))
          {
              e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
              e.Graphics.FillPath(highlightBrush, path);
          }
      }
  }
  protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
  {
      Color textColor = Color.White;
      if (!e.Item.Enabled)
      {
          textColor = Color.Gray;
      }
      Font textFont = e.TextFont ?? SystemFonts.DefaultFont;
      using (SolidBrush textBrush = new SolidBrush(textColor))
      using (StringFormat sf = new StringFormat())
      {
          if ((e.TextFormat & TextFormatFlags.Right) == TextFormatFlags.Right)
          {
              sf.Alignment = StringAlignment.Far;
          }
          else if ((e.TextFormat & TextFormatFlags.HorizontalCenter) == TextFormatFlags.HorizontalCenter)
          {
              sf.Alignment = StringAlignment.Center;
          }
          else
          {
              sf.Alignment = StringAlignment.Near;
          }
          if ((e.TextFormat & TextFormatFlags.Bottom) == TextFormatFlags.Bottom)
          {
              sf.LineAlignment = StringAlignment.Far;
          }
          else if ((e.TextFormat & TextFormatFlags.VerticalCenter) == TextFormatFlags.VerticalCenter)
          {
              sf.LineAlignment = StringAlignment.Center;
          }
          else
          {
              sf.LineAlignment = StringAlignment.Near;
          }
          if ((e.TextFormat & TextFormatFlags.WordBreak) != TextFormatFlags.WordBreak)
          {
              sf.FormatFlags |= StringFormatFlags.NoWrap;
          }
          if ((e.TextFormat & TextFormatFlags.EndEllipsis) == TextFormatFlags.EndEllipsis)
          {
              sf.Trimming = StringTrimming.EllipsisCharacter;
              sf.FormatFlags |= StringFormatFlags.NoWrap;
          }
          else if ((e.TextFormat & TextFormatFlags.PathEllipsis) == TextFormatFlags.PathEllipsis)
          {
              sf.Trimming = StringTrimming.EllipsisPath;
              sf.FormatFlags |= StringFormatFlags.NoWrap;
          }
          else if ((e.TextFormat & TextFormatFlags.WordEllipsis) == TextFormatFlags.WordEllipsis)
          {
              sf.Trimming = StringTrimming.EllipsisWord;
              sf.FormatFlags |= StringFormatFlags.NoWrap;
          }
          e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
          e.Graphics.DrawString(e.Text, textFont, textBrush, e.TextRectangle, sf);
      }
  }
  protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
  {
      base.OnRenderItemImage(e);
  }
  protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
  {}
  protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
  {
      Rectangle rect = new Rectangle(Point.Empty, e.Item.Size);
      int y = rect.Height / 2;
      using (Pen separatorPen = new Pen(Color.FromArgb(80, 255, 255, 255)))
      {
          e.Graphics.DrawLine(separatorPen, 4, y, rect.Width - 4, y);
      }
  }
  protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
  {
      using (SolidBrush brush = new SolidBrush(Color.Black))
      {
          e.Graphics.FillRectangle(brush, e.AffectedBounds);
      }
  }
  private GraphicsPath RoundedRect(Rectangle bounds, int radius)
  {
      int d = radius * 2;
      GraphicsPath path = new GraphicsPath();
      path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
      path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
      path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
      path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
      path.CloseFigure();
      return path;
  }
}
public class HamsterViewer : Form
{
  private Point mouseOffset;
  private bool isMouseDown = false;
  private Dictionary<string, List<AnimationFrame>> loadedAnimations = new Dictionary<string, List<AnimationFrame>>();
  private List<AnimationFrame> currentAnimationFrames = new List<AnimationFrame>();
  private int currentFrameIndex = 0;
  private Bitmap? currentFrameBitmap = null;
  private System.Windows.Forms.Timer? animationTimer = null;
  private Random random = new Random();
  private string currentAnimationName = "AnimMainIdle";
  private int idleLoopCounter = 0;
  private const int maxIdleLoops = 1;
  private bool isRandomIdleSequence = false;
  private string currentIdleStart = "";
  private string currentIdleLoop = "";
  private string currentIdleFinish = "";
  private bool isSpotifyMusicPlaying = false;
  private string currentMusicStart = "AnimMusicStart";
  private string currentMusicLoop = "AnimMusicLoop";
  private string currentMusicFinish = "AnimMusicFinish";
  private System.Windows.Forms.Timer? musicCheckTimer = null;
  private bool isDraggingFile = false;
  private bool isCharacterDraggingAnimation = false;
  private List<string> oneOffRandomIdleAnimations = new List<string>
  {
      "AnimIdle1", "AnimIdle3",
      "AnimIdle4", "AnimIdle5", "AnimIdle6"
  };
  private HashSet<string> uninterruptibleAnimations = new HashSet<string>();
  private ContextMenuStrip contextMenu;
  private ToolStripMenuItem exitMenuItem;
  private ToolStripMenuItem textMenuItem;
  private ToolStripSeparator menuSeparator;
  private const int WS_EX_LAYERED = 0x00080000;
  private const int WS_EX_TOOLWINDOW = 0x80;
  private const int WS_EX_TOPMOST = 0x00000008;
  private const byte AC_SRC_OVER = 0x00;
  private const byte AC_SRC_ALPHA = 0x01;
  private const int ULW_ALPHA = 0x00000002;
  private const string SpotifyAumidSubstring = "Spotify";
  [StructLayout(LayoutKind.Sequential)]
  private struct PointStruct
  {
      public int x;
      public int y;
  }
  [StructLayout(LayoutKind.Sequential)]
  private struct SizeStruct
  {
      public int cx;
      public int cy;
  }
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  private struct BLENDFUNCTION
  {
      public byte BlendOp;
      public byte BlendFlags;
      public byte SourceConstantAlpha;
      public byte AlphaFormat;
  }
  [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
  private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref PointStruct pptDst, ref SizeStruct psize, IntPtr hdcSrc, ref PointStruct pptSrc, uint crKey, [In] ref BLENDFUNCTION pblend, uint dwFlags);
  [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
  private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
  [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
  private static extern bool DeleteDC(IntPtr hdc);
  [DllImport("gdi32.dll", ExactSpelling = true)]
  private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
  [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
  private static extern IntPtr GetDC(IntPtr hWnd);
  [DllImport("user32.dll", ExactSpelling = true)]
  private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
  [DllImport("gdi32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  public static extern bool DeleteObject([In] IntPtr hObject);
  private const int WH_KEYBOARD_LL = 13;
  private const int WM_KEYDOWN = 0x0100;
  private const int WM_SYSKEYDOWN = 0x0104;
  [StructLayout(LayoutKind.Sequential)]
  private struct KBDLLHOOKSTRUCT
  {
      public uint vkCode;
      public uint scanCode;
      public uint flags;
      public uint time;
      public IntPtr dwExtraInfo;
  }
  private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
  [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
  private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
  [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static extern bool UnhookWindowsHookEx(IntPtr hhk);
  [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
  private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
  [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
  private static extern IntPtr GetModuleHandle(string lpModuleName);
  private IntPtr _hookID = IntPtr.Zero;
  private LowLevelKeyboardProc? _proc = null;
  private DateTime lastKeyPressTime = DateTime.MinValue;
  private DateTime typingSessionStartTime = DateTime.MinValue;
  private System.Windows.Forms.Timer? typingCheckTimer = null;
  private const int typingDurationThresholdMs = 2000;
  private bool isTypingAnimationActive = false;
  public HamsterViewer()
  {
      FormBorderStyle = FormBorderStyle.None;
      TopMost = true;
      ShowInTaskbar = false;
      StartPosition = FormStartPosition.CenterScreen;
      AllowDrop = true;
      this.MouseDown += Form_MouseDown;
      this.MouseMove += Form_MouseMove;
      this.MouseUp += Form_MouseUp;
      this.DragEnter += HamsterViewer_DragEnter;
      this.DragDrop += HamsterViewer_DragDrop;
      this.DragLeave += HamsterViewer_DragLeave;
      contextMenu = new ContextMenuStrip();
      contextMenu.ShowImageMargin = true;
      contextMenu.ShowCheckMargin = false;
      contextMenu.ImageScalingSize = new Size(16, 16);
      var customRenderer = new AnimatedMenuRenderer();
      exitMenuItem = new ToolStripMenuItem("выйти");
      try
      {
          string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "files", "icon1.ico");
          if (File.Exists(iconPath))
          {
              using (var stream = new FileStream(iconPath, FileMode.Open, FileAccess.Read))
              {
                  exitMenuItem.Image = new Bitmap(stream);
              }
          }
          else
          {
              string[] possiblePaths = {
                  Path.Combine(Directory.GetCurrentDirectory(), "files", "icon1.ico"),
                  Path.Combine(Directory.GetCurrentDirectory(), "icon1.ico"),
                  "ani/icon1.ico",
                  "icon1.ico"
              };
              foreach (string path in possiblePaths)
              {
                  if (File.Exists(path))
                  {
                      using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                      {
                          exitMenuItem.Image = new Bitmap(stream);
                      }
                      break;
                  }
              }
          }
      }
      catch (Exception ex)
      {
          Debug.WriteLine(ex.Message + "\n" + ex.StackTrace);
      }
      exitMenuItem.Click += ExitMenuItem_Click;
      var linkMenuItem = new ToolStripMenuItem("отправить мне донат");
      try
      {
          string iconPath2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "files", "icon_2.ico");
          if (File.Exists(iconPath2))
          {
              using (var stream = new FileStream(iconPath2, FileMode.Open, FileAccess.Read))
              {
                  linkMenuItem.Image = new Bitmap(stream);
              }
          }
          else
          {
              string[] possiblePaths2 = {
                  Path.Combine(Directory.GetCurrentDirectory(), "files", "icon_2.ico"),
                  Path.Combine(Directory.GetCurrentDirectory(), "icon_2.ico"),
                  "ani/icon_2.ico",
                  "icon_2.ico"
              };
              foreach (string path in possiblePaths2)
              {
                  if (File.Exists(path))
                  {
                      using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                      {
                          linkMenuItem.Image = new Bitmap(stream);
                      }
                      break;
                  }
              }
          }
      }
      catch (Exception ex)
      {
          Debug.WriteLine($"{ex.Message}\n{ex.StackTrace}");
      }
      linkMenuItem.Click += (s, e) =>
      {
          try
          {
              Process.Start(new ProcessStartInfo
              {
                  FileName = "https://www.donationalerts.com/r/just_blaing__",
                  UseShellExecute = true
              });
          }
          catch (Exception ex)
          {
              Debug.WriteLine($"{ex.Message}");
              MessageBox.Show("Не удалось открыть ссылку :(", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
          }
      };
      menuSeparator = new ToolStripSeparator();
      textMenuItem = new ToolStripMenuItem("chomik v 1.1.1");
      textMenuItem.Enabled = true;
      textMenuItem.Click += TextMenuItem_Click;
      customRenderer.ItemsToExcludeFromHighlight.Add(textMenuItem);
      contextMenu.Renderer = customRenderer;
      contextMenu.BackColor = Color.Black;
      contextMenu.Items.Add(exitMenuItem);
      contextMenu.Items.Add(linkMenuItem);
      contextMenu.Items.Add(menuSeparator);
      contextMenu.Items.Add(textMenuItem);
      this.ContextMenuStrip = contextMenu;
      PopulateUninterruptibleAnimations();
      try
      {
          PreloadAnimations();
          LoadInitialAnimation();
          animationTimer = new System.Windows.Forms.Timer();
          animationTimer.Tick += AnimationTimer_Tick;
          if (currentAnimationFrames.Count > 0 && currentFrameBitmap != null)
          {
              UpdateWindowVisuals(currentFrameBitmap);
              animationTimer.Interval = currentAnimationFrames[0].Duration > 0 ? currentAnimationFrames[0].Duration : 100;
              animationTimer.Start();
          }
          musicCheckTimer = new System.Windows.Forms.Timer();
          musicCheckTimer.Interval = 500;
          musicCheckTimer.Tick += MusicCheckTimer_Tick;
          musicCheckTimer.Start();
          typingCheckTimer = new System.Windows.Forms.Timer();
          typingCheckTimer.Interval = 100;
          typingCheckTimer.Tick += TypingCheckTimer_Tick;
          typingCheckTimer.Start();
          _ = CheckMusicStateAsync();
          _proc = HookCallback;
          _hookID = SetHook(_proc);
      }
      catch (FileNotFoundException ex)
      {
          MessageBox.Show($"файл ненайдн {ex.Message}", "отвал", MessageBoxButtons.OK, MessageBoxIcon.Error);
          Application.Exit();
      }
      catch (Exception ex)
      {
          MessageBox.Show($"отвал: {ex.Message}", "отвал", MessageBoxButtons.OK, MessageBoxIcon.Error);
          Application.Exit();
      }
  }
  private IntPtr SetHook(LowLevelKeyboardProc proc)
  {
      using (Process curProcess = Process.GetCurrentProcess())
      using (ProcessModule? curModule = curProcess.MainModule)
      {
          if (curModule?.ModuleName != null)
          {
              return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
          }
          return IntPtr.Zero;
      }
  }
  private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
  {
      if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
      {
          KBDLLHOOKSTRUCT kbStruct = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT))!;
          uint vkCode = kbStruct.vkCode;

          if (vkCode >= 0x41 && vkCode <= 0x5A)
          {
              lastKeyPressTime = DateTime.Now;
          }
      }
      return CallNextHookEx(_hookID, nCode, wParam, lParam);
  }
  private void TypingCheckTimer_Tick(object? sender, EventArgs e)
  {
      if (isCharacterDraggingAnimation) return;
      TimeSpan elapsedSinceLastKeyPress = DateTime.Now - lastKeyPressTime;
      bool isUserCurrentlyTypingLetters = elapsedSinceLastKeyPress.TotalMilliseconds < typingDurationThresholdMs;
      if (isSpotifyMusicPlaying || isDraggingFile)
      {
          if (isTypingAnimationActive)
          {
              isTypingAnimationActive = false;
              typingSessionStartTime = DateTime.MinValue;
              if (currentAnimationName == "AnimTypingStart" || currentAnimationName == "AnimTyping" || currentAnimationName == "AnimTypingStop")
              {
                  HandleAnimationFinish();
              }
          }
          return;
      }
      if (isUserCurrentlyTypingLetters)
      {
          if (!isTypingAnimationActive)
          {
              if (typingSessionStartTime == DateTime.MinValue)
              {
                  typingSessionStartTime = DateTime.Now;
              }
              TimeSpan elapsedSinceTypingStart = DateTime.Now - typingSessionStartTime;
              if (elapsedSinceTypingStart.TotalMilliseconds >= typingDurationThresholdMs)
              {
                  if (loadedAnimations.ContainsKey("AnimTypingStart"))
                  {
                      LoadAnimation("AnimTypingStart");
                      currentAnimationName = "AnimTypingStart";
                  }
                  else if (loadedAnimations.ContainsKey("AnimTyping"))
                  {
                      LoadAnimation("AnimTyping");
                      currentAnimationName = "AnimTyping";
                  }
                  isTypingAnimationActive = true;
              }
          }
          else
          {
              if (currentAnimationName == "AnimTypingStart" && currentFrameIndex >= currentAnimationFrames.Count - 1)
              {
                  if (loadedAnimations.ContainsKey("AnimTyping"))
                  {
                      LoadAnimation("AnimTyping");
                      currentAnimationName = "AnimTyping";
                  }
              }
          }
      }
      else
      {
          if (isTypingAnimationActive)
          {
              if (currentAnimationName != "AnimTypingStop")
              {
                  if (loadedAnimations.ContainsKey("AnimTypingStop"))
                  {
                      LoadAnimation("AnimTypingStop");
                      currentAnimationName = "AnimTypingStop";
                  }
                  else
                  {
                      isTypingAnimationActive = false;
                      HandleAnimationFinish();
                  }
              }
          }
          typingSessionStartTime = DateTime.MinValue;
      }
  }
  private void PopulateUninterruptibleAnimations()
  {
      uninterruptibleAnimations.Clear();
      uninterruptibleAnimations.Add("AnimIdleStart1");
      uninterruptibleAnimations.Add("AnimIdleStart2");
      uninterruptibleAnimations.Add("AnimIdleStart3");
      uninterruptibleAnimations.Add("AnimIdleFinish1");
      uninterruptibleAnimations.Add("AnimIdleFinish2");
      uninterruptibleAnimations.Add("AnimIdleFinish3");
      foreach (var animName in oneOffRandomIdleAnimations)
      {
          uninterruptibleAnimations.Add(animName);
      }
      uninterruptibleAnimations.Add("AnimTypingStart");
      uninterruptibleAnimations.Add("AnimTyping");
      uninterruptibleAnimations.Add("AnimTypingStop");
      uninterruptibleAnimations.Add(currentMusicStart);
      uninterruptibleAnimations.Add(currentMusicLoop);
      uninterruptibleAnimations.Add(currentMusicFinish);
      uninterruptibleAnimations.Add("AnimDragFileStart");
      uninterruptibleAnimations.Add("AnimDragFileProcessing");
      uninterruptibleAnimations.Add("AnimDragFileFinish");
  }
  protected override CreateParams CreateParams
  {
      get
      {
          CreateParams cp = base.CreateParams;
          cp.ExStyle |= WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
          return cp;
      }
  }
  protected override void OnPaintBackground(PaintEventArgs e) { }
  protected override void OnPaint(PaintEventArgs e) { }
  private void UpdateWindowVisuals(Bitmap bitmap)
  {
      if (bitmap == null || !this.IsHandleCreated) return;
      IntPtr screenDc = GetDC(IntPtr.Zero);
      IntPtr memDc = CreateCompatibleDC(screenDc);
      IntPtr hBitmap = IntPtr.Zero;
      IntPtr oldBitmap = IntPtr.Zero;
      try
      {
          hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
          oldBitmap = SelectObject(memDc, hBitmap);
          SizeStruct size = new SizeStruct();
          size.cx = bitmap.Width;
          size.cy = bitmap.Height;
          PointStruct pointSource = new PointStruct();
          pointSource.x = 0;
          pointSource.y = 0;
          PointStruct topPos = new PointStruct();
          topPos.x = this.Left;
          topPos.y = this.Top;
          BLENDFUNCTION blend = new BLENDFUNCTION();
          blend.BlendOp = AC_SRC_OVER;
          blend.BlendFlags = 0;
          blend.SourceConstantAlpha = 255;
          blend.AlphaFormat = AC_SRC_ALPHA;
          UpdateLayeredWindow(this.Handle, screenDc, ref topPos, ref size, memDc, ref pointSource, 0, ref blend, ULW_ALPHA);
          if (this.ClientSize.Width != bitmap.Width || this.ClientSize.Height != bitmap.Height)
          {
              this.ClientSize = new Size(bitmap.Width, bitmap.Height);
          }
      }
      catch (Exception ex)
      {
          Debug.WriteLine($"{ex.Message}");
      }
      finally
      {
          ReleaseDC(IntPtr.Zero, screenDc);
          if (hBitmap != IntPtr.Zero)
          {
              SelectObject(memDc, oldBitmap);
              DeleteObject(hBitmap);
          }
          DeleteDC(memDc);
      }
  }
  protected override void OnShown(EventArgs e)
  {
      base.OnShown(e);
  }
  private void ExitMenuItem_Click(object? sender, EventArgs e)
  {
      Application.Exit();
  }
  private void TextMenuItem_Click(object? sender, EventArgs e)
  {
      List<AnimationFrame>? musicLoopFrames = null;
      if (loadedAnimations.ContainsKey("AnimMusicLoop"))
      {
          musicLoopFrames = loadedAnimations["AnimMusicLoop"];
      }
      using (var customMessageBox = new CustomMessageBox("created with love❤\nauthor: blaing", musicLoopFrames))
      {
          customMessageBox.ShowDialog(this);
      }
  }
  private void PreloadAnimations()
  {
      loadedAnimations.Clear();
      string animsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "files", "anims.txt");
      string aniFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "files");
      if (!File.Exists(animsFilePath))
      {
          animsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "anims.txt");
          if (!File.Exists(animsFilePath))
          {
              MessageBox.Show("Файл с анимациями (anims.txt) не найден :(", "отвал", MessageBoxButtons.OK, MessageBoxIcon.Error);
              Application.Exit();
          }
      }
      if (!Directory.Exists(aniFolderPath))
      {
          aniFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "files");
          if (!Directory.Exists(aniFolderPath))
          {
              aniFolderPath = AppDomain.CurrentDomain.BaseDirectory;
          }
      }
      try
      {
          string[] lines = File.ReadAllLines(animsFilePath);
          string? currentAnimSection = null;
          List<AnimationFrame>? currentAnimFramesList = null;
          foreach (string line in lines)
          {
              string trimmedLine = line.Trim();
              if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("//") || trimmedLine.StartsWith("#"))
              {
                  continue;
              }

              if (trimmedLine.StartsWith("Anim"))
              {
                  if (currentAnimSection != null && currentAnimFramesList != null && currentAnimFramesList.Count > 0)
                  {
                      loadedAnimations[currentAnimSection] = currentAnimFramesList;
                  }
                  currentAnimSection = trimmedLine;
                  currentAnimFramesList = new List<AnimationFrame>();
              }
              else if (currentAnimSection != null && currentAnimFramesList != null)
              {
                  if (int.TryParse(trimmedLine, out _))
                  {
                      continue;
                  }
                  string[] parts = trimmedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                  if (parts.Length >= 2 && parts[0].EndsWith(".png", StringComparison.OrdinalIgnoreCase) && int.TryParse(parts[1], out int duration))
                  {
                      string frameFileName = parts[0];
                      string framePath = Path.Combine(aniFolderPath, frameFileName);

                      if (File.Exists(framePath))
                      {
                          using (var stream = new FileStream(framePath, FileMode.Open, FileAccess.Read))
                          {
                              Bitmap frameBitmap = new Bitmap(stream);
                              currentAnimFramesList.Add(new AnimationFrame(frameBitmap, duration));
                          }
                      }
                      else
                      {
                          Debug.WriteLine($"файл кадра не найден: {framePath} для анимации {currentAnimSection}");
                      }
                  }
              }
          }
          if (currentAnimSection != null && currentAnimFramesList != null && currentAnimFramesList.Count > 0)
          {
              loadedAnimations[currentAnimSection] = currentAnimFramesList;
          }
      }
      catch (Exception ex)
      {
          MessageBox.Show($"Ошибка при загрузке анимаций: {ex.Message} :(", "отвал", MessageBoxButtons.OK, MessageBoxIcon.Error);
          Application.Exit();
      }
  }
  private async void MusicCheckTimer_Tick(object? sender, EventArgs e)
  {
      await CheckMusicStateAsync();
  }
  private async Task CheckMusicStateAsync()
  {
      bool previousSpotifyMusicState = isSpotifyMusicPlaying;
      bool currentSpotifyMusicState = false;
      try
      {
          var sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
          var currentSession = sessionManager.GetCurrentSession();
          if (currentSession != null)
          {
              var playbackInfo = currentSession.GetPlaybackInfo();
              if (playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
              {
                  if (currentSession.SourceAppUserModelId.Contains(SpotifyAumidSubstring, StringComparison.OrdinalIgnoreCase))
                  {
                      currentSpotifyMusicState = true;
                  }
              }
          }
      }
      catch (Exception)
      {
          currentSpotifyMusicState = false;
      }
      isSpotifyMusicPlaying = currentSpotifyMusicState;
      if (isSpotifyMusicPlaying != previousSpotifyMusicState && !isCharacterDraggingAnimation && !isDraggingFile)
      {
          if (isSpotifyMusicPlaying)
          {
              if (loadedAnimations.ContainsKey(currentMusicStart))
              {
                  LoadAnimation(currentMusicStart);
                  currentAnimationName = currentMusicStart;
              }
              else if (loadedAnimations.ContainsKey(currentMusicLoop))
              {
                  LoadAnimation(currentMusicLoop);
                  currentAnimationName = currentMusicLoop;
              }
          }
          else
          {
              if (currentAnimationName == currentMusicStart || currentAnimationName == currentMusicLoop)
              {
                  if (loadedAnimations.ContainsKey(currentMusicFinish))
                  {
                      LoadAnimation(currentMusicFinish);
                      currentAnimationName = currentMusicFinish;
                  }
                  else
                  {
                      HandleAnimationFinish();
                  }
              }
          }
      }
  }
  private void LoadInitialAnimation()
  {
      isRandomIdleSequence = false;
      idleLoopCounter = 0;
      currentIdleStart = "";
      currentIdleLoop = "";
      currentIdleFinish = "";
      isTypingAnimationActive = false;
      lastKeyPressTime = DateTime.MinValue;
      typingSessionStartTime = DateTime.MinValue;
      isDraggingFile = false;
      isCharacterDraggingAnimation = false;
      _ = CheckMusicStateAsync().ContinueWith(task =>
      {
          if (isSpotifyMusicPlaying)
          {
              if (loadedAnimations.ContainsKey(currentMusicLoop))
              {
                  LoadAnimation(currentMusicLoop);
                  currentAnimationName = currentMusicLoop;
              }
              else if (loadedAnimations.ContainsKey(currentMusicStart))
              {
                  LoadAnimation(currentMusicStart);
                  currentAnimationName = currentMusicStart;
              }
              else
              {
                  LoadAnimation("AnimMainIdle");
                  currentAnimationName = "AnimMainIdle";
              }
          }
          else
          {
              LoadAnimation("AnimMainIdle");
              currentAnimationName = "AnimMainIdle";
          }
      }, TaskScheduler.FromCurrentSynchronizationContext());
  }
  private void LoadAnimation(string animationName)
  {
      animationTimer?.Stop();
      isCharacterDraggingAnimation = animationName == "AnimCharacterMoveStart" ||
                                     animationName == "AnimCharacterMoving" ||
                                     animationName == "AnimCharacterMoveFinish";
      if (loadedAnimations.TryGetValue(animationName, out var frames) && frames.Count > 0)
      {
          currentAnimationFrames = frames;
          currentFrameIndex = 0;
          try
          {
              currentFrameBitmap = currentAnimationFrames[0].Image;
              UpdateWindowVisuals(currentFrameBitmap);
              if (animationTimer != null)
              {
                  int interval = currentAnimationFrames[0].Duration;
                  animationTimer.Interval = interval > 0 ? interval : 100;
                  animationTimer.Start();
              }
          }
          catch (Exception ex)
          {
              Debug.WriteLine($"ошибка при загрузке первого кадра анимации '{animationName}': {ex.Message}");
              if (animationName != "AnimMainIdle")
              {
                  currentAnimationName = "AnimMainIdle";
                  LoadAnimation("AnimMainIdle");
              }
              else
              {
                  MessageBox.Show($"Не удалось загрузить основную анимацию :(", "ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  Application.Exit();
              }
          }
      }
      else
      {
          Debug.WriteLine($"анимация '{animationName}' не найдена");
          if (animationName != "AnimMainIdle")
          {
              currentAnimationName = "AnimMainIdle";
              LoadAnimation("AnimMainIdle");
          }
          else
          {
              MessageBox.Show("Анимация 'AnimMainIdle' отсутствует или недействительна :(", "ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
              animationTimer?.Stop();
              currentAnimationFrames.Clear();
              currentFrameBitmap = null;
              Application.Exit();
          }
      }
  }
  private void AnimationTimer_Tick(object? sender, EventArgs e)
  {
      if (currentAnimationFrames == null || currentAnimationFrames.Count == 0 || animationTimer == null)
      {
          animationTimer?.Stop();
          Debug.WriteLine("ошибко с таймером или кадрами анимации.");
          if (currentAnimationName != "AnimMainIdle")
          {
              LoadAnimation("AnimMainIdle");
              currentAnimationName = "AnimMainIdle";
          }
          return;
      }
      try
      {
          int previousFrameIndex = currentFrameIndex;
          currentFrameIndex++;
          bool animationFinished = false;
          if (currentAnimationName == "AnimCharacterMoving" && isMouseDown)
          {
               if (currentFrameIndex >= currentAnimationFrames.Count)
               {
                   currentFrameIndex = 0;
               }
          }
          else if (currentAnimationName == currentMusicLoop && isSpotifyMusicPlaying)
          {
              if (currentFrameIndex >= currentAnimationFrames.Count)
              {
                  currentFrameIndex = 0;
              }
          }
          else if (currentAnimationName == "AnimTyping" && isTypingAnimationActive)
          {
              if (currentFrameIndex >= currentAnimationFrames.Count)
              {
                  currentFrameIndex = 0;
              }
          }
           else if (currentAnimationName == "AnimDragFileProcessing" && isDraggingFile)
          {
              if (currentFrameIndex >= currentAnimationFrames.Count)
              {
                  currentFrameIndex = 0;
              }
          }
          else if (isRandomIdleSequence && currentAnimationName == currentIdleLoop && idleLoopCounter < maxIdleLoops)
          {
               if (currentFrameIndex >= currentAnimationFrames.Count)
               {
                   currentFrameIndex = 0;
                   idleLoopCounter++;
               }
          }
          if (currentFrameIndex >= currentAnimationFrames.Count)
          {
              animationFinished = true;
          }
          if (animationFinished)
          {
              HandleAnimationFinish();
              return;
          }
          if (currentFrameIndex < 0 || currentFrameIndex >= currentAnimationFrames.Count)
          {
               Debug.WriteLine($"ошибка: индекс кадра вне диапазона ({currentFrameIndex}) для анимации '{currentAnimationName}'. Сброс.");
               HandleAnimationFinish();
               return;
          }
          AnimationFrame frame = currentAnimationFrames[currentFrameIndex];
          currentFrameBitmap = frame.Image;
          UpdateWindowVisuals(currentFrameBitmap);
          int interval = frame.Duration;
          animationTimer.Interval = interval > 0 ? interval : 100;
      }
      catch (ObjectDisposedException)
      {
          animationTimer?.Stop();
      }
      catch (Exception ex)
      {
          Debug.WriteLine($"ошибка при отображении кадра: {ex.Message}");
          animationTimer?.Stop();
          if (currentAnimationName != "AnimMainIdle")
          {
              LoadAnimation("AnimMainIdle");
              currentAnimationName = "AnimMainIdle";
          }
      }
  }
  private void HandleAnimationFinish()
  {
      string previousAnimationName = currentAnimationName;
      string nextAnimation = "AnimMainIdle";

      if (previousAnimationName == "AnimCharacterMoveStart")
      {
          if (isMouseDown && loadedAnimations.ContainsKey("AnimCharacterMoving"))
          {
              nextAnimation = "AnimCharacterMoving";
          }
          else if (!isMouseDown && loadedAnimations.ContainsKey("AnimCharacterMoveFinish"))
          {
              nextAnimation = "AnimCharacterMoveFinish";
          }
          else
          {
              nextAnimation = "AnimMainIdle";
          }
      }
      else if (previousAnimationName == "AnimCharacterMoving")
      {
          if (!isMouseDown && loadedAnimations.ContainsKey("AnimCharacterMoveFinish"))
          {
              nextAnimation = "AnimCharacterMoveFinish";
          }
          else
          {
              nextAnimation = "AnimMainIdle";
          }
      }
      else if (previousAnimationName == "AnimCharacterMoveFinish")
      {
          nextAnimation = "AnimMainIdle";
      }
      else if (previousAnimationName == "AnimTypingStop")
      {
          isTypingAnimationActive = false;
          typingSessionStartTime = DateTime.MinValue;
          if (isSpotifyMusicPlaying && loadedAnimations.ContainsKey(currentMusicLoop)) nextAnimation = currentMusicLoop;
          else if (isDraggingFile && loadedAnimations.ContainsKey("AnimDragFileProcessing")) nextAnimation = "AnimDragFileProcessing";
          else nextAnimation = "AnimMainIdle";
      }
      else if (previousAnimationName == "AnimDragFileStart")
      {
          if (isDraggingFile && loadedAnimations.ContainsKey("AnimDragFileProcessing"))
          {
              nextAnimation = "AnimDragFileProcessing";
          }
          else if (!isDraggingFile && loadedAnimations.ContainsKey("AnimDragFileFinish"))
          {
              nextAnimation = "AnimDragFileFinish";
          }
          else
          {
              isDraggingFile = false;
              nextAnimation = "AnimMainIdle";
          }
      }
      else if (previousAnimationName == "AnimDragFileProcessing" || previousAnimationName == "AnimDragFileFinish")
      {
          isDraggingFile = false;
          if (isSpotifyMusicPlaying && loadedAnimations.ContainsKey(currentMusicLoop)) nextAnimation = currentMusicLoop;
          else if (isTypingAnimationActive && loadedAnimations.ContainsKey("AnimTyping")) nextAnimation = "AnimTyping";
          else nextAnimation = "AnimMainIdle";
      }
      else if (previousAnimationName == currentMusicFinish)
      {
          isSpotifyMusicPlaying = false;
          if (isDraggingFile && loadedAnimations.ContainsKey("AnimDragFileProcessing")) nextAnimation = "AnimDragFileProcessing";
          else if (isTypingAnimationActive && loadedAnimations.ContainsKey("AnimTyping")) nextAnimation = "AnimTyping";
          else nextAnimation = "AnimMainIdle";
      }
      else if (isRandomIdleSequence && previousAnimationName == currentIdleFinish)
      {
          isRandomIdleSequence = false;
          currentIdleStart = ""; currentIdleLoop = ""; currentIdleFinish = "";
          idleLoopCounter = 0;
          if (isSpotifyMusicPlaying && loadedAnimations.ContainsKey(currentMusicLoop)) nextAnimation = currentMusicLoop;
          else if (isDraggingFile && loadedAnimations.ContainsKey("AnimDragFileProcessing")) nextAnimation = "AnimDragFileProcessing";
          else if (isTypingAnimationActive && loadedAnimations.ContainsKey("AnimTyping")) nextAnimation = "AnimTyping";
          else nextAnimation = "AnimMainIdle";
      }
      else if (isRandomIdleSequence && previousAnimationName == currentIdleStart)
      {
           if (loadedAnimations.ContainsKey(currentIdleLoop))
           {
               nextAnimation = currentIdleLoop;
               idleLoopCounter = 1;
           }
           else if (loadedAnimations.ContainsKey(currentIdleFinish))
           {
               nextAnimation = currentIdleFinish;
               idleLoopCounter = 0;
           }
           else
           {
               isRandomIdleSequence = false;
               nextAnimation = "AnimMainIdle";
           }
      }
       else if (isRandomIdleSequence && previousAnimationName == currentIdleLoop && idleLoopCounter >= maxIdleLoops)
      {
          if (loadedAnimations.ContainsKey(currentIdleFinish))
          {
              nextAnimation = currentIdleFinish;
          }
          else
          {
              isRandomIdleSequence = false;
              nextAnimation = "AnimMainIdle";
          }
      }
      else if (oneOffRandomIdleAnimations.Contains(previousAnimationName))
      {
          if (isSpotifyMusicPlaying && loadedAnimations.ContainsKey(currentMusicLoop)) nextAnimation = currentMusicLoop;
          else if (isDraggingFile && loadedAnimations.ContainsKey("AnimDragFileProcessing")) nextAnimation = "AnimDragFileProcessing";
          else if (isTypingAnimationActive && loadedAnimations.ContainsKey("AnimTyping")) nextAnimation = "AnimTyping";
          else nextAnimation = "AnimMainIdle";
      }
      else
      {
          if (isSpotifyMusicPlaying && loadedAnimations.ContainsKey(currentMusicLoop)) nextAnimation = currentMusicLoop;
          else if (isDraggingFile && loadedAnimations.ContainsKey("AnimDragFileProcessing")) nextAnimation = "AnimDragFileProcessing";
          else if (isTypingAnimationActive && loadedAnimations.ContainsKey("AnimTyping")) nextAnimation = "AnimTyping";
          else if (isRandomIdleSequence)
          {
               isRandomIdleSequence = false;
               nextAnimation = "AnimMainIdle";
          }
          else
          {
              if (random.Next(100) < 20 && oneOffRandomIdleAnimations.Any(a => loadedAnimations.ContainsKey(a)))
              {
                  var availableRandomIdles = oneOffRandomIdleAnimations.Where(a => loadedAnimations.ContainsKey(a)).ToList();
                  if (availableRandomIdles.Count > 0) nextAnimation = availableRandomIdles[random.Next(availableRandomIdles.Count)];
                  else nextAnimation = "AnimMainIdle";
              }
              else if (random.Next(100) < 10)
              {
                  var availableStartAnims = loadedAnimations.Keys.Where(k => k.StartsWith("AnimIdleStart")).ToList();
                  if (availableStartAnims.Count > 0)
                  {
                      currentIdleStart = availableStartAnims[random.Next(availableStartAnims.Count)];
                      if (int.TryParse(currentIdleStart.Replace("AnimIdleStart", ""), out int startNumber))
                      {
                          currentIdleLoop = $"AnimIdleLoop{startNumber}";
                          currentIdleFinish = $"AnimIdleFinish{startNumber}";

                          if (loadedAnimations.ContainsKey(currentIdleStart))
                          {
                              nextAnimation = currentIdleStart;
                              isRandomIdleSequence = true;
                              idleLoopCounter = 0;
                          }
                          else if (loadedAnimations.ContainsKey(currentIdleLoop))
                          {
                              nextAnimation = currentIdleLoop;
                              isRandomIdleSequence = true;
                              idleLoopCounter = 1;
                          }
                          else if (loadedAnimations.ContainsKey(currentIdleFinish))
                          {
                              nextAnimation = currentIdleFinish;
                              isRandomIdleSequence = true;
                              idleLoopCounter = 0;
                          }
                          else
                          {
                              nextAnimation = "AnimMainIdle";
                              isRandomIdleSequence = false;
                          }
                      }
                      else
                      {
                           nextAnimation = "AnimMainIdle";
                           isRandomIdleSequence = false;
                      }
                  }
                  else
                  {
                      nextAnimation = "AnimMainIdle";
                      isRandomIdleSequence = false;
                  }
              }
              else
              {
                  nextAnimation = "AnimMainIdle";
              }
          }
      }
      bool isLoopingAnimation = currentAnimationName == "AnimCharacterMoving" ||
                                currentAnimationName == currentMusicLoop ||
                                currentAnimationName == "AnimTyping" ||
                                currentAnimationName == "AnimDragFileProcessing" ||
                                (isRandomIdleSequence && currentAnimationName == currentIdleLoop);
      if (nextAnimation != currentAnimationName || !isLoopingAnimation)
      {
           LoadAnimation(nextAnimation);
           currentAnimationName = nextAnimation;
      }
      else
      {
           if (animationTimer != null && !animationTimer.Enabled)
           {
               animationTimer.Start();
           }
      }
      isCharacterDraggingAnimation = currentAnimationName == "AnimCharacterMoveStart" ||
                                     currentAnimationName == "AnimCharacterMoving" ||
                                     currentAnimationName == "AnimCharacterMoveFinish";
      isRandomIdleSequence = currentAnimationName.StartsWith("AnimIdleStart") ||
                             currentAnimationName.StartsWith("AnimIdleLoop") ||
                             currentAnimationName.StartsWith("AnimIdleFinish");
      isTypingAnimationActive = currentAnimationName == "AnimTypingStart" ||
                                currentAnimationName == "AnimTyping" ||
                                currentAnimationName == "AnimTypingStop";
  }
  private void Form_MouseDown(object? sender, MouseEventArgs e)
  {
      if (e.Button == MouseButtons.Left)
      {
          mouseOffset = new Point(-e.X, -e.Y);
          isMouseDown = true;

          if (!isSpotifyMusicPlaying && !isTypingAnimationActive && !isDraggingFile &&
              !uninterruptibleAnimations.Contains(currentAnimationName) &&
              !currentAnimationName.StartsWith("AnimCharacterMove"))
          {
              if (loadedAnimations.ContainsKey("AnimCharacterMoveStart"))
              {
                  LoadAnimation("AnimCharacterMoveStart");
                  currentAnimationName = "AnimCharacterMoveStart";
              }
              else if (loadedAnimations.ContainsKey("AnimCharacterMoving"))
              {
                  LoadAnimation("AnimCharacterMoving");
                  currentAnimationName = "AnimCharacterMoving";
              }
          }
      }
  }
  private void Form_MouseMove(object? sender, MouseEventArgs e)
  {
      if (isMouseDown)
      {
          Point mousePos = Control.MousePosition;
          mousePos.Offset(mouseOffset.X, mouseOffset.Y);
          Location = mousePos;

          if (isMouseDown && !isDraggingFile && !isSpotifyMusicPlaying && !isTypingAnimationActive &&
               currentAnimationName != "AnimCharacterMoveStart" &&
               currentAnimationName != "AnimCharacterMoving" &&
               currentAnimationName != "AnimCharacterMoveFinish" &&
               loadedAnimations.ContainsKey("AnimCharacterMoving"))
          {
               LoadAnimation("AnimCharacterMoving");
               currentAnimationName = "AnimCharacterMoving";
          }
      }
  }
  protected override void OnLocationChanged(EventArgs e)
  {
      base.OnLocationChanged(e);
      if (currentFrameBitmap != null)
      {
          UpdateWindowVisuals(currentFrameBitmap);
      }
  }
  private void Form_MouseUp(object? sender, MouseEventArgs e)
  {
      if (e.Button == MouseButtons.Left)
      {
          isMouseDown = false;

          if (isCharacterDraggingAnimation)
          {
              if (loadedAnimations.ContainsKey("AnimCharacterMoveFinish"))
              {
                  LoadAnimation("AnimCharacterMoveFinish");
                  currentAnimationName = "AnimCharacterMoveFinish";
              }
              else
              {
                  HandleAnimationFinish();
              }
          }
      }
  }
  private void HamsterViewer_DragEnter(object? sender, DragEventArgs e)
  {
      if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
      {
          if (isDraggingFile)
          {
              e.Effect = DragDropEffects.None;
              return;
          }
          e.Effect = DragDropEffects.Copy;
          if (isCharacterDraggingAnimation)
          {
               isMouseDown = false;
          }
          if (!isSpotifyMusicPlaying && !currentAnimationName.StartsWith("AnimDragFile"))
          {
              isDraggingFile = true;
              if (loadedAnimations.ContainsKey("AnimDragFileStart"))
              {
                  LoadAnimation("AnimDragFileStart");
                  currentAnimationName = "AnimDragFileStart";
              }
              else if (loadedAnimations.ContainsKey("AnimDragFileProcessing"))
              {
                  LoadAnimation("AnimDragFileProcessing");
                  currentAnimationName = "AnimDragFileProcessing";
              }
          }
      }
      else
      {
          e.Effect = DragDropEffects.None;
      }
  }
  private void HamsterViewer_DragDrop(object? sender, DragEventArgs e)
  {
      if (isDraggingFile && e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
      {
          string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];
          if (files != null && files.Length > 0)
          {
              if (loadedAnimations.ContainsKey("AnimDragFileFinish"))
              {
                  LoadAnimation("AnimDragFileFinish");
                  currentAnimationName = "AnimDragFileFinish";
              }
              else
              {
                  HandleAnimationFinish();
              }
          }
          else
          {
               if (loadedAnimations.ContainsKey("AnimDragFileFinish"))
               {
                   LoadAnimation("AnimDragFileFinish");
                   currentAnimationName = "AnimDragFileFinish";
               }
               else
               {
                   HandleAnimationFinish();
               }
          }
      }
      else if (isDraggingFile)
      {
          if (loadedAnimations.ContainsKey("AnimDragFileFinish"))
          {
              LoadAnimation("AnimDragFileFinish");
              currentAnimationName = "AnimDragFileFinish";
          }
          else
          {
              HandleAnimationFinish();
          }
      }
  }
  private void HamsterViewer_DragLeave(object? sender, EventArgs e)
  {
      if (isDraggingFile)
      {
          if (loadedAnimations.ContainsKey("AnimDragFileFinish"))
          {
              if (currentAnimationName == "AnimDragFileStart" || currentAnimationName == "AnimDragFileProcessing")
              {
                  LoadAnimation("AnimDragFileFinish");
                  currentAnimationName = "AnimDragFileFinish";
              }
          }
          else
          {
              if (currentAnimationName == "AnimDragFileStart" || currentAnimationName == "AnimDragFileProcessing")
              {
                  HandleAnimationFinish();
              }
          }
      }
  }
  protected override void OnFormClosing(FormClosingEventArgs e)
  {
      base.OnFormClosing(e);
      animationTimer?.Stop();
      animationTimer?.Dispose();
      musicCheckTimer?.Stop();
      musicCheckTimer?.Dispose();
      typingCheckTimer?.Stop();
      typingCheckTimer?.Dispose();
      if (_hookID != IntPtr.Zero)
      {
          UnhookWindowsHookEx(_hookID);
          _hookID = IntPtr.Zero;
      }
      _proc = null;
      foreach (var animationEntry in loadedAnimations)
      {
          foreach (var frame in animationEntry.Value)
          {
              frame.Image?.Dispose();
          }
      }
      loadedAnimations.Clear();
      textMenuItem?.Dispose();
      menuSeparator?.Dispose();
      exitMenuItem?.Dispose();
      contextMenu?.Dispose();
  }
}
