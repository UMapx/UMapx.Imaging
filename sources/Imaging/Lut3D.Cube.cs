using System;
using System.Globalization;
using System.IO;

namespace UMapx.Imaging
{
    public partial class Lut3D
    {
        #region Cube components
        /// <summary>
        /// Loads a three-dimensional .cube lookup table from a file.
        /// </summary>
        /// <param name="path">Path to the .cube file.</param>
        /// <returns>Lookup-table filter.</returns>
        public static Lut3D FromCube(string path)
        {
            using (var reader = new StreamReader(path)) return FromCube(reader);
        }

        /// <summary>
        /// Reads an Adobe/IRIDAS three-dimensional .cube lookup table. The reader remains open.
        /// Supports TITLE, LUT_3D_SIZE, DOMAIN_MIN/MAX and the Resolve LUT_3D_INPUT_RANGE extension.
        /// One-dimensional tables and combined 1D/3D tables are not supported.
        /// </summary>
        /// <param name="reader">Text reader.</param>
        /// <returns>Lookup-table filter.</returns>
        public static Lut3D FromCube(TextReader reader)
        {
            var result = new Lut3D();
            float[,,,] data = null;
            int size = 0, count = 0, lineNumber = 0;
            bool hasTitle = false, hasMin = false, hasMax = false;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                bool quoted = false;
                for (int i = 0; i < line.Length; i++)
                {
                    if (line[i] == '"') quoted = !quoted;
                    else if (line[i] == '#' && !quoted) { line = line.Substring(0, i); break; }
                }
                line = line.Trim().TrimStart('\uFEFF').TrimStart();
                if (line.Length == 0) continue;
                string[] parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                string key = parts[0];
                FormatException Error(string message) => new FormatException("Invalid .cube at line " + lineNumber + ": " + message);
                float Number(string token)
                {
                    if (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                        || float.IsNaN(value) || float.IsInfinity(value)) throw Error("Expected a finite number.");
                    return value;
                }
                switch (key)
                {
                    case "TITLE":
                        if (hasTitle || count != 0) throw Error("Repeated or misplaced TITLE.");
                        string title = line.Substring(key.Length).Trim();
                        if (title.Length < 2 || title[0] != '"' || title[title.Length - 1] != '"')
                            throw Error("TITLE must be quoted.");
                        result.Title = title.Substring(1, title.Length - 2);
                        hasTitle = true;
                        break;
                    case "LUT_3D_SIZE":
                        if (data != null || count != 0 || parts.Length != 2
                            || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out size)
                            || size < 2 || size > 256) throw Error("LUT_3D_SIZE must occur once and be in [2, 256].");
                        data = new float[size, size, size, 3];
                        break;
                    case "DOMAIN_MIN":
                    case "DOMAIN_MAX":
                        bool min = key == "DOMAIN_MIN";
                        if (count != 0 || parts.Length != 4 || (min ? hasMin : hasMax))
                            throw Error("Repeated or malformed domain bounds.");
                        float[] bounds = { Number(parts[1]), Number(parts[2]), Number(parts[3]) };
                        if (min) { result.domainMin = bounds; hasMin = true; }
                        else { result.domainMax = bounds; hasMax = true; }
                        break;
                    case "LUT_3D_INPUT_RANGE":
                        if (count != 0 || hasMin || hasMax || parts.Length != 3)
                            throw Error("Repeated or malformed input range.");
                        float low = Number(parts[1]), high = Number(parts[2]);
                        result.domainMin = new[] { low, low, low };
                        result.domainMax = new[] { high, high, high };
                        hasMin = hasMax = true;
                        break;
                    case "LUT_1D_SIZE":
                    case "LUT_1D_INPUT_RANGE":
                        throw new NotSupportedException("Only three-dimensional .cube lookup tables are supported.");
                    default:
                        if (data == null || parts.Length != 3 || count >= size * size * size)
                            throw Error("Expected LUT_3D_SIZE followed by exactly N cubed RGB entries.");
                        int r = count % size, g = count / size % size, b = count / (size * size);
                        for (int c = 0; c < 3; c++) data[r, g, b, c] = Number(parts[c]);
                        count++;
                        break;
                }
            }
            if (data == null || count != size * size * size)
                throw new FormatException("The .cube file must contain LUT_3D_SIZE and exactly N cubed RGB entries.");
            for (int c = 0; c < 3; c++)
                if (result.domainMin[c] >= result.domainMax[c])
                    throw new FormatException("Each DOMAIN_MIN component must be less than DOMAIN_MAX.");
            result.table = data;
            result.rebuild = true;
            return result;
        }
        #endregion
    }
}
