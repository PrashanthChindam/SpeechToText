using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Enums;

namespace Common.Utilities
{
    public static class PrintLib
    {
        /// <summary>
        /// Prints the possible content of input text to Page as per the space availablity and returns back the left over text
        /// </summary>
        /// <param name="stringToPrint"></param>
        /// <param name="graphics"></param>
        /// <param name="printFont"></param>
        /// <param name="brush"></param>
        /// <param name="rectF"></param>
        /// <param name="textJustification"></param>
        /// <returns></returns>
        public static string PrintPage(string stringToPrint, Graphics graphics, Font printFont, Brush brush, RectangleF rectF, TextJustification textJustification)
        {
            return DrawParagraphs(stringToPrint, graphics, printFont, brush, rectF, TextJustification.Full, 1, 1, 0);
        }

        // Draw justified text on the Graphics
        // object in the indicated Rectangle.
        private static string DrawParagraphs(string text, Graphics gr,
            Font font, Brush brush, RectangleF rect,
            TextJustification justification, float line_spacing,
            float indent, float paragraph_spacing)
        {
            string totalLeftOverText = "";
            string paraLeftOverText = "";

            // Split the text into paragraphs.
            string[] paragraphs = text.Split('\n');
            int paraCurrentIndex = 0;

            // Draw each paragraph.
            while (paraCurrentIndex < paragraphs.Length)
            {
                // Draw the paragraph keeping track of remaining space.
                (rect, paraLeftOverText) = DrawParagraph(gr, rect, font, brush,
                    paragraphs[paraCurrentIndex], justification, line_spacing,
                    indent, paragraph_spacing);

                // See if there's any room left.
                if (rect.Height < font.Size || !string.IsNullOrEmpty(paraLeftOverText))
                {
                    totalLeftOverText = (string.IsNullOrEmpty(paraLeftOverText)) ? "" : (paraLeftOverText + "\n");
                    if (paraCurrentIndex < paragraphs.Length - 1)
                    {
                        paraCurrentIndex = paraCurrentIndex + 1;
                        totalLeftOverText = totalLeftOverText + string.Join("\n", paragraphs, paraCurrentIndex, paragraphs.Length - paraCurrentIndex);
                    }

                    break;
                }

                paraCurrentIndex = paraCurrentIndex + 1;
            }

            return totalLeftOverText;
        }

        // Draw a paragraph by lines inside the Rectangle.
        // Return a RectangleF representing any unused
        // space in the original RectangleF.
        private static (RectangleF, string) DrawParagraph(Graphics gr, RectangleF rect,
            Font font, Brush brush, string text,
            TextJustification justification, float line_spacing,
            float indent, float extra_paragraph_spacing)
        {
            string leftOverText = "";

            // Get the coordinates for the first line.
            float y = rect.Top;

            // Break the text into words.
            string[] words = text.Split(' ');
            int currentWordIndex = 0;
            int endWordIndex = 0;

            // Repeat until we run out of text or room.
            for (; ; )
            {
                // See how many words will fit.
                // Start with just the next word.
                string line = words[currentWordIndex];

                // Add more words until the line won't fit.
                endWordIndex = currentWordIndex + 1;
                while (endWordIndex < words.Length)
                {
                    // See if the next word fits.
                    string test_line = line + " " + words[endWordIndex];
                    SizeF line_size = gr.MeasureString(test_line, font);
                    if (line_size.Width > rect.Width)
                    {
                        // The line is too wide. Don't use the last word.
                        endWordIndex--;
                        break;
                    }
                    else
                    {
                        // The word fits. Save the test line.
                        line = test_line;
                    }

                    // Try the next word.
                    endWordIndex++;
                }

                // See if this is the last line in the paragraph.
                if ((endWordIndex == words.Length) &&
                    (justification == TextJustification.Full))
                {
                    // This is the last line. Don't justify it.
                    DrawLine(gr, line, font, brush,
                        rect.Left + indent,
                        y,
                        rect.Width - indent,
                        TextJustification.Left);
                }
                else
                {
                    // This is not the last line. Justify it.
                    DrawLine(gr, line, font, brush,
                        rect.Left + indent,
                        y,
                        rect.Width - indent,
                        justification);
                }

                // Move down to draw the next line.
                y += font.Height * line_spacing;

                // Make sure there's room for another line.
                if (y > rect.Bottom)
                    break;

                // Start the next line at the next word.
                currentWordIndex = endWordIndex + 1;
                if (currentWordIndex >= words.Length) break;

                // Don't indent subsequent lines in this paragraph.
                indent = 0;
            }            

            // Add a gap after the paragraph.
            y += font.Height * extra_paragraph_spacing;

            // Return a RectangleF representing any unused
            // space in the original RectangleF.
            float height = rect.Bottom - y - (float)0.5;
            if (height < 0)
                height = 0;

            // left over text
            if (endWordIndex < words.Length)
            {
                endWordIndex = endWordIndex + 1;
                leftOverText = string.Join(" ", words, endWordIndex, words.Length - endWordIndex);
            }

            return (new RectangleF(rect.X, y, rect.Width, height), leftOverText);
        }
        // Draw a line of text.
        private static void DrawLine(Graphics gr, string line, Font font,
            Brush brush, float x, float y, float width,
            TextJustification justification)
        {
            // Make a rectangle to hold the text.
            RectangleF rect = new RectangleF(x, y, width, font.Height);

            // See if we should use full justification.
            if (justification == TextJustification.Full)
            {
                // Justify the text.
                DrawJustifiedLine(gr, rect, font, brush, line);
            }
            else
            {
                // Make a StringFormat to align the text.
                using (StringFormat sf = new StringFormat())
                {
                    // Use the appropriate alignment.
                    switch (justification)
                    {
                        case TextJustification.Left:
                            sf.Alignment = StringAlignment.Near;
                            break;
                        case TextJustification.Right:
                            sf.Alignment = StringAlignment.Far;
                            break;
                        case TextJustification.Center:
                            sf.Alignment = StringAlignment.Center;
                            break;
                    }

                    gr.DrawString(line, font, brush, rect, sf);
                }
            }
        }

        // Draw justified text on the Graphics object
        // in the indicated Rectangle.
        private static void DrawJustifiedLine(Graphics gr, RectangleF rect,
            Font font, Brush brush, string text)
        {
            // Break the text into words.
            string[] words = text.Split(' ');

            // Add a space to each word and get their lengths.
            float[] word_width = new float[words.Length];
            float total_width = 0;
            for (int i = 0; i < words.Length; i++)
            {
                // See how wide this word is.
                SizeF size = gr.MeasureString(words[i], font);
                word_width[i] = size.Width;
                total_width += word_width[i];
            }

            // Get the additional spacing between words.
            float extra_space = rect.Width - total_width;
            int num_spaces = words.Length - 1;
            if (words.Length > 1) extra_space /= num_spaces;

            // Draw the words.
            float x = rect.Left;
            float y = rect.Top;
            for (int i = 0; i < words.Length; i++)
            {
                // Draw the word.
                gr.DrawString(words[i], font, brush, x, y);

                // Move right to draw the next word.
                x += word_width[i] + extra_space;
            }
        }

    }
}
