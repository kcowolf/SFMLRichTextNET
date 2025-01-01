/*
C# Port: https://github.com/kcowolf/SFMLRichTextNET
    Benjamin Stauffer

Original C++ implementation: https://github.com/skyrpex/RichText
    Cristian Pallarés - Original code
    Lukas Dürrenberger - Conversion to the new SFML 2 API
*/

using SFML.Graphics;
using SFML.System;
using System.Text;

namespace SFMLRichTextNET
{
    public class TextStroke
    {
        public Color Fill { get; set; } = Color.White;

        public Color Outline { get; set; } = Color.Transparent;

        public float Thickness { get; set; } = 0.0f;
    }

    public class Outline
    {
        public Color Color { get; set; } = Color.Transparent;

        public float Thickness { get; set; } = 0.0f;
    }

    public class RichText : Transformable, Drawable
    {
        public class Line : Transformable, Drawable
        {
            public void SetCharacterColor(int pos, Color color)
            {
                IsolateCharacter(pos);
                var stringToFormat = ConvertLinePosToLocal(pos);
                m_texts[stringToFormat].FillColor = color;
                UpdateGeometry();
            }

            public void SetCharacterStyle(int pos, Text.Styles style)
            {
                IsolateCharacter(pos);
                var stringToFormat = ConvertLinePosToLocal(pos);
                m_texts[stringToFormat].Style = style;
                UpdateGeometry();
            }

            public void SetCharacter(int pos, char character)
            {
                var charArray = m_texts[ConvertLinePosToLocal(pos)].DisplayedString.ToCharArray();
                charArray[pos] = character;
                m_texts[ConvertLinePosToLocal(pos)].DisplayedString = new string(charArray);
                UpdateGeometry();
            }

            public void SetCharacterSize(uint size)
            {
                foreach (var text in m_texts)
                {
                    text.CharacterSize = size;
                }

                UpdateGeometry();
            }

            public void SetFont(Font font)
            {
                foreach (var text in m_texts)
                {
                    text.Font = font;
                }

                UpdateGeometry();
            }

            public int GetLength()
            {
                var count = 0;

                foreach (var text in m_texts)
                {
                    count += text.DisplayedString.Length;
                }

                return count;
            }

            public Color GetCharacterColor(int pos)
            {
                return m_texts[ConvertLinePosToLocal(pos)].FillColor;
            }

            public Text.Styles GetCharacterStyle(int pos)
            {
                return m_texts[ConvertLinePosToLocal(pos)].Style;
            }

            public char GetCharacter(int pos)
            {
                return m_texts[ConvertLinePosToLocal(pos)].DisplayedString[pos];
            }

            public List<Text> GetTexts()
            {
                return m_texts;
            }

            public void AppendText(Text text)
            {
                UpdateTextAndGeometry(text);
                m_texts.Add(text);
            }

            public FloatRect GetLocalBounds()
            {
                return m_bounds;
            }

            public FloatRect GetGlobalBounds()
            {
                return Transform.TransformRect(m_bounds);
            }

            public void Draw(RenderTarget target, RenderStates states)
            {
                states.Transform *= Transform;

                foreach (var text in m_texts)
                {
                    target.Draw(text, states);
                }
            }

            private int ConvertLinePosToLocal(int pos)
            {
                int arrayIndex = 0;
                for (; pos >= m_texts[arrayIndex].DisplayedString.Length; ++arrayIndex)
                {
                    pos -= m_texts[arrayIndex].DisplayedString.Length;
                }

                return arrayIndex;
            }

            private void IsolateCharacter(int pos)
            {
                var localPos = pos;
                var index = ConvertLinePosToLocal(localPos);
                var temp = m_texts[index];

                var str = temp.DisplayedString;
                if (str.Length == 1)
                {
                    return;
                }

                m_texts.RemoveAt(index);
                if (localPos != str.Length - 1)
                {
                    temp.DisplayedString = str.Substring(localPos + 1);
                    m_texts.Insert(index, temp);
                }

                temp.DisplayedString = str.Substring(localPos, 1);
                m_texts.Insert(index, temp);

                if (localPos != 0)
                {
                    temp.DisplayedString = str.Substring(0, localPos);
                    m_texts.Insert(index, temp);
                }
            }

            private void UpdateGeometry()
            {
                m_bounds = new FloatRect();

                foreach (var text in m_texts)
                {
                    UpdateTextAndGeometry(text);
                }
            }

            private void UpdateTextAndGeometry(Text text)
            {
                text.Position = new Vector2f(m_bounds.Width, 0.0f);

                // Update bounds
                float lineSpacing = (float)Math.Floor(text.Font.GetLineSpacing(text.CharacterSize));
                m_bounds.Height = Math.Max(m_bounds.Height, lineSpacing);
                m_bounds.Width += text.GetGlobalBounds().Width;
            }

            private readonly List<Text> m_texts = [];
            private FloatRect m_bounds;
        }

        public RichText() : this(null)
        {
        }

        public RichText(Font? font)
        {
            m_font = font;
            m_characterSize = 30;
            m_currentStroke = new TextStroke
            {
                Fill = Color.White,
                Outline = Color.Transparent
            };
            m_currentStyle = Text.Styles.Regular;
        }

        public static RichText operator<<(RichText richText, TextStroke stroke)
        {
            richText.m_currentStroke = stroke;
            return richText;
        }

        public static RichText operator<<(RichText richText, Outline outline)
        {
            richText.m_currentStroke.Outline = outline.Color;
            richText.m_currentStroke.Thickness = outline.Thickness;
            return richText;
        }

        public static RichText operator<<(RichText richText, Color color)
        {
            richText.m_currentStroke.Fill = color;
            return richText;
        }

        public static RichText operator<<(RichText richText, Text.Styles style)
        {
            richText.m_currentStyle = style;
            return richText;
        }

        private static List<string> Explode(string str, char delimiter)
        {
            if (str.Length == 0)
            {
                return [];
            }

            // For each character in the string
            var result = new List<string>();
            var buffer = new StringBuilder();
            foreach (var character in str.ToCharArray())
            {
                if (character == delimiter)
                {
                    // Add them to the result vector
                    result.Add(buffer.ToString());
                    buffer.Clear();
                }
                else
                {
                    // Accumulate the next character into the sequence
                    buffer.Append(character);
                }
            }

            // Add to the result if buffer still has contents or if the last character is a delimiter
            if (buffer.Length != 0 || str[^1] == delimiter)
            {
                result.Add(buffer.ToString());
            }

            return result;
        }

        public static RichText operator<<(RichText richText, string str)
        {
            // Maybe skip
            if (str.Length == 0)
            {
                return richText;
            }

            // Explode into substrings
            var subStrings = Explode(str, '\n');

            var it = subStrings.GetEnumerator();

            // Append first substring using the last line
            if (it.MoveNext())
            {
                // If there isn't any line, just create it
                if (richText.m_lines.Count == 0)
                {
                    richText.m_lines.Add(new Line());
                }

                // Remove last line's height
                var line = richText.m_lines.Last();
                richText.m_bounds.Height -= line.GetGlobalBounds().Height;

                // Append text
                line.AppendText(richText.CreateText(it.Current));

                // Update bounds
                richText.m_bounds.Height += line.GetGlobalBounds().Height;
                richText.m_bounds.Width = Math.Max(richText.m_bounds.Width, line.GetGlobalBounds().Width);
            }

            // Append the rest of substrings as new lines
            while (it.MoveNext())
            {
                var line = new Line()
                {
                    Position = new Vector2f(0.0f, richText.m_bounds.Height)
                };

                line.AppendText(richText.CreateText(it.Current));
                richText.m_lines.Add(line);

                // Update bounds
                richText.m_bounds.Height += line.GetGlobalBounds().Height;
                richText.m_bounds.Width = Math.Max(richText.m_bounds.Width, line.GetGlobalBounds().Width);
            }

            // Return
            return richText;
        }

        public void SetCharacterColor(int line, int pos, Color color)
        {
            m_lines[line].SetCharacterColor(pos, color);
            UpdateGeometry();
        }

        public void SetCharacterStyle(int line, int pos, Text.Styles style)
        {
            m_lines[line].SetCharacterStyle(pos, style);
            UpdateGeometry();
        }

        public void SetCharacter(int line, int pos, char character)
        {
            m_lines[line].SetCharacter(pos, character);
            UpdateGeometry();
        }

        public void SetCharacterSize(uint size)
        {
            // Maybe skip
            if (m_characterSize == size)
            {
                return;
            }

            // Update character size
            m_characterSize = size;

            // Set texts character size
            foreach (var line in m_lines)
            {
                line.SetCharacterSize(size);
            }

            UpdateGeometry();
        }

        public void SetFont(Font font)
        {
            // Maybe skip
            if (m_font == font)
            {
                return;
            }

            m_font = font;

            foreach (var line in m_lines)
            {
                line.SetFont(font);
            }

            UpdateGeometry();
        }

        public void Clear()
        {
            // Clear texts
            m_lines.Clear();

            // Reset bounds
            m_bounds = new FloatRect();
        }

        public Color GetCharacterColor(int line, int pos)
        {
            return m_lines[line].GetCharacterColor(pos);
        }

        public Text.Styles GetCharacterStyle(int line, int pos)
        {
            return m_lines[line].GetCharacterStyle(pos);
        }

        public char GetCharacter(int line, int pos)
        {
            return m_lines[line].GetCharacter(pos);
        }

        public List<Line> GetLines()
        {
            return m_lines;
        }

        public uint GetCharacterSize()
        {
            return m_characterSize;
        }

        public Font? GetFont()
        {
            return m_font;
        }

        public FloatRect GetLocalBounds()
        {
            return m_bounds;
        }

        public FloatRect GetGlobalBounds()
        {
            return Transform.TransformRect(m_bounds);
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
            states.Transform *= Transform;

            foreach (var line in m_lines)
            {
                target.Draw(line, states);
            }
        }

        private Text CreateText(string str)
        {
            var text = new Text()
            {
                DisplayedString = str,
                FillColor = m_currentStroke.Fill,
                OutlineColor = m_currentStroke.Outline,
                OutlineThickness = m_currentStroke.Thickness,
                Style = m_currentStyle,
                CharacterSize = m_characterSize
            };

            if (m_font != null)
            {
                text.Font = m_font;
            }

            return text;
        }

        private void UpdateGeometry()
        {
            m_bounds = new FloatRect();

            foreach (var line in m_lines)
            {
                line.Position = new Vector2f(0.0f, m_bounds.Height);
                m_bounds.Height += line.GetGlobalBounds().Height;
                m_bounds.Width = Math.Max(m_bounds.Width, line.GetGlobalBounds().Width);
            }
        }

        private readonly List<Line> m_lines = [];
        private Font? m_font;
        private uint m_characterSize;
        private FloatRect m_bounds;
        private TextStroke m_currentStroke;
        private Text.Styles m_currentStyle;
    }
}
