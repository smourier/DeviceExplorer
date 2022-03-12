using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace DeviceExplorer.Utilities
{
    public class ByteArrayControl : ScrollViewer, IDisposable
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(object), typeof(ByteArrayControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, OnSourceChanged));

        public static readonly DependencyProperty RowCountProperty = DependencyProperty.Register(nameof(RowCount), typeof(int), typeof(ByteArrayControl),
            new FrameworkPropertyMetadata(16, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, OnRowCountChanged, OnRowCountCoerce));

        public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(nameof(Offset), typeof(long), typeof(ByteArrayControl),
            new FrameworkPropertyMetadata(0L, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, OnOffsetChanged, OnOffsetCoerce));

        public static readonly DependencyProperty AddHeaderProperty = DependencyProperty.Register(nameof(AddHeader), typeof(bool), typeof(ByteArrayControl),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, OnAddHeaderChanged));

        private bool _disposedValue;
        private readonly Panel _canvas;
        private readonly TextBlock _text;
        private bool _dontDisposeStream;
        private Stream _stream;
        private ScrollBar _verticalScrollBar;
        private byte[] _buffer;

        public object Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
        public long Offset { get => (long)GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }
        public int RowCount { get => (int)GetValue(RowCountProperty); set => SetValue(RowCountProperty, value); }
        public bool AddHeader { get => (bool)GetValue(AddHeaderProperty); set => SetValue(AddHeaderProperty, value); }

        public ByteArrayControl()
        {
            Margin = new Thickness(5);
            VerticalAlignment = VerticalAlignment.Top;
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            FontFamily = new FontFamily("Lucida Console");

            // NOTE: text is *not* a canvas child because there are rounding issues with big (really big) values for Canvas.SetTop...
            _text = new TextBlock();

            _canvas = new Canvas();
            Content = _canvas;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            Size size = base.ArrangeOverride(arrangeSize);
            _text.Arrange(new Rect(size));

            // adjust canvas width so the h scrollbar can automatically appear
            _canvas.Width = _text.DesiredSize.Width + HorizontalOffset;

            var bufferSize = ComputeNeededBufferSize();
            if (_buffer == null || _buffer.Length != bufferSize)
            {
                _buffer = new byte[bufferSize];
            }
            return size;
        }

        private int ComputeNeededBufferSize()
        {
            if (_text.FontSize == 0 || DesiredSize.Height == 0)
                return 512; // whatever...

            // it's in fact a bit more than what's really needed (if the h scrollbar is displayed, roundings, etc.)
            var maxLines = DesiredSize.Height / _text.FontSize;
            return RowCount * (int)maxLines;
        }

        private static object OnRowCountCoerce(DependencyObject d, object baseValue)
        {
            var i = (int)baseValue;
            if (i < 8)
                return 8;

            if (i > 256)
                return 256;

            return i;
        }

        private static object OnOffsetCoerce(DependencyObject d, object baseValue)
        {
            var bac = (ByteArrayControl)d;
            if (bac._stream == null)
                return 0L;

            var l = (long)baseValue;
            if (l > bac._stream.Length || l < 0)
                return bac._stream.Length;

            return l;
        }

        private static void OnOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var bac = (ByteArrayControl)d;
            bac.ScrollToVerticalOffset((long)e.NewValue);
        }

        private static void OnAddHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var bac = (ByteArrayControl)d;
            bac.ResizeContent();
            bac.SetupText();
        }

        private static void OnRowCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var bac = (ByteArrayControl)d;
            bac.ResizeContent();
            bac.SetupText();
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var bac = (ByteArrayControl)d;
            bac.OpenStream(e.NewValue);
            bac.ResizeContent();
            bac.SetupText();
        }

        protected virtual void ResizeContent()
        {
            _text.FontSize = FontSize;
            _text.FontFamily = FontFamily;
            _text.FontStretch = FontStretch;
            _text.FontWeight = FontWeight;
            _text.Foreground = Foreground;
            _text.Background = Background;
            _text.FontStyle = FontStyle;
            _text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            _canvas.Height = _stream == null ? 0 : _stream.Length + _text.FontSize;
            _canvas.Width = _text.DesiredSize.Width + HorizontalOffset;
        }

        protected virtual void SetupText()
        {
            if (_stream == null)
            {
                _text.Text = null;
                if (VerticalScrollBar != null)
                {
                    VerticalScrollBar.ToolTip = null;
                }
                return;
            }

            // read bytes and set the text content with it
            var pos = _stream.Position;
            var read = _buffer != null ? _stream.Read(_buffer, 0, _buffer.Length) : 0;
            _text.Text = ToHexaDump(_buffer, 0, read, AddHeader, RowCount, pos);

            // display some tooltip
            if (VerticalScrollBar != null)
            {
                VerticalScrollBar.ToolTip = _stream.Position + " / " + _stream.Length + " (" + (100 * _stream.Position) / _stream.Length + "%)";
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (base.VisualChildrenCount > index)
                return base.GetVisualChild(index);

            return _text;
        }

        protected override int VisualChildrenCount => base.VisualChildrenCount + 1;

        private ScrollBar VerticalScrollBar
        {
            get
            {
                if (_verticalScrollBar == null && Template != null)
                {
                    _verticalScrollBar = Template.FindName("PART_VerticalScrollBar", this) as ScrollBar;
                }
                return _verticalScrollBar;
            }
        }

        protected override void OnScrollChanged(ScrollChangedEventArgs e)
        {
            base.OnScrollChanged(e);
            if (_stream == null)
                return;

            var pos = _stream.Position;
            var newPos = ScrollableHeight == 0 ? (long)e.VerticalOffset : (long)((e.VerticalOffset * (double)(_stream.Length - ComputeNeededBufferSize() / 2)) / ScrollableHeight);
            newPos -= (newPos % RowCount);
            if (pos != newPos || newPos == 0)
            {
                _stream.Position = newPos;
                SetupText();
            }
            _text.Margin = new Thickness(-e.HorizontalOffset, 0, 0, 0);
        }

        private static bool MustWrapStream(Stream stream)
        {
            if (!stream.CanSeek)
                return true;

            try
            {
                var pos = stream.Position;
                var len = stream.Length;
                return false;
            }
            catch
            {
                return true;
            }
        }

        protected virtual void OpenStream(object source)
        {
            Dispose();
            if (source == null)
                return;

            if (source is Stream stream)
            {
                if (!stream.CanRead)
                    throw new ArgumentException(null, nameof(source));

                if (MustWrapStream(stream))
                {
                    _stream = new MemoryStream();
                    stream.CopyTo(_stream);
                    _stream.Position = 0;
                    return;
                }

                _dontDisposeStream = true;
                _stream = stream;
                return;
            }

            if (source is string filePath && File.Exists(filePath))
            {
                _stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return;
            }

            var uri = source as Uri;
            if (uri != null && uri.IsFile && File.Exists(uri.LocalPath))
            {
                _stream = new FileStream(uri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return;
            }

            if (source is byte[] bytes)
            {
                _stream = new MemoryStream(bytes)
                {
                    Position = 0
                };
                return;
            }
        }

        public static string ToHexaDump(byte[] bytes) => ToHexaDump(bytes, 0, bytes != null ? bytes.Length : 0, true, 16, 0);
        public static string ToHexaDump(byte[] bytes, int offset, int count, bool addHeader, int rowCount, long address)
        {
            bytes ??= Array.Empty<byte>();

            if (offset < 0)
            {
                offset = 0;
            }

            if (count < 0)
            {
                count = bytes.Length;
            }

            if ((offset + count) > bytes.Length)
            {
                count = bytes.Length - offset;
            }

            var b16 = address >= int.MaxValue;
            var format = b16 ? "{0:X16}  " : "{0:X8}  ";

            var sb = new StringBuilder();
            if (addHeader)
            {
                // Offset    00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F  0123456789ABCDEF
                // --------  -----------------------------------------------  ----------------

                sb.Append("Offset  ");
                if (b16)
                {
                    sb.Append("        ");
                }
                sb.Append("  ");
                for (var i = 0; i < rowCount; i++)
                {
                    sb.AppendFormat("{0:X2} ", i);
                }
                sb.Append(' ');
                for (var i = 0; i < rowCount; i++)
                {
                    sb.AppendFormat("{0:X1}", (i % 16));
                }
                sb.AppendLine();

                sb.AppendFormat("--------");
                if (b16)
                {
                    sb.AppendFormat("--------");
                }
                sb.Append("  ");
                for (var i = 0; i < (rowCount * 3) - 1; i++)
                {
                    sb.Append('-');
                }
                sb.Append("  ");
                for (var i = 0; i < rowCount; i++)
                {
                    sb.Append('-');
                }
                sb.AppendLine();
            }

            for (var i = 0; i < count; i += rowCount)
            {
                sb.AppendFormat(format, i + offset + address);

                int j;
                for (j = 0; (j < rowCount) && ((i + j) < count); j++)
                {
                    sb.AppendFormat("{0:X2} ", bytes[i + j + offset]);
                }
                sb.Append(' ');

                if (j < rowCount)
                {
                    sb.Append(new string(' ', 3 * (rowCount - j)));
                }

                for (j = 0; j < rowCount && (i + j) < count; j++)
                {
                    var b = bytes[i + j + offset];
                    var c = (char)b;
                    if (b > 31 && b < 128)
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        sb.Append('.');
                    }
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects).
                    if (_stream != null && !_dontDisposeStream)
                    {
                        _stream.Dispose();
                    }
                    _stream = null;
                    _dontDisposeStream = false;
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.
                // set large fields to null.

                _disposedValue = true;
            }
        }

        ~ByteArrayControl()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
