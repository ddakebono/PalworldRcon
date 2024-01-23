using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace PalworldRcon;

public class ConsoleWriter : TextWriter
{
    private TextBlock _textbox;
    private ScrollViewer _scrollViewer;
    public ConsoleWriter(TextBlock textbox, ScrollViewer scrollViewer)
    {
        _textbox = textbox;
        _scrollViewer = scrollViewer;
    }

    public override void Write(char value)
    {
        _textbox.Text += value;
        _scrollViewer.ScrollToBottom();
    }

    public override void Write(string value)
    {
        _textbox.Text += value;
        _scrollViewer.ScrollToBottom();
    }

    public override Encoding Encoding => Encoding.UTF8;
}