/*
C# Port: https://github.com/kcowolf/SFMLRichTextNET
    Benjamin Stauffer

Original C++ implementation: https://github.com/skyrpex/RichText
    Cristian Pallarés - Original code
    Lukas Dürrenberger - Conversion to the new SFML 2 API
*/

using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace SFMLRichTextNET
{
    public class Program
    {
        static void OnClose(object sender, EventArgs e)
        {
            ((RenderWindow)sender).Close();
        }

        static void OnKey(object sender, KeyEventArgs e)
        {
            if (e.Scancode == Keyboard.Scancode.Escape)
            {
                ((RenderWindow)sender).Close();
            }
        }

        static void Main(string[] args)
        {
            var window = new RenderWindow(new VideoMode(800, 600), "RichText in SFML.NET");
            window.SetFramerateLimit(30);

            window.Closed += new EventHandler(OnClose!);
            window.KeyPressed += new EventHandler<KeyEventArgs>(OnKey!);

            var font = new Font("LiberationSans-Regular.ttf");

            var text = new RichText(font);
            text = text << Text.Styles.Bold << Color.Cyan << "This "
                 << Text.Styles.Italic << Color.White << "is\nan\n"
                 << Text.Styles.Regular << Color.Green << "example"
                 << Color.White << ".\n"
                 << Text.Styles.Underlined << "It looks good!\n" << Text.Styles.StrikeThrough
                 << new Outline { Color = Color.Blue, Thickness = 3.0f } << "Really good!";

            text.SetCharacterSize(25);
            text.Position = new Vector2f(400, 300);
            text.Origin = new Vector2f(text.GetGlobalBounds().Width / 2.0f, text.GetGlobalBounds().Height / 2.0f);

            while (window.IsOpen)
            {
                window.DispatchEvents();
                window.Clear();
                window.Draw(text);
                window.Display();
            }
        }
    }
}
