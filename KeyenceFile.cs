/*
 *  $Id: keyence.c 18034 2016-01-08 14:02:03Z yeti-dn $
 *  Copyright (C) 2015 David Necas (Yeti).
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin Street, Fifth Floor,
 *  Boston, MA 02110-1301, USA.
 */

/**
 * [FILE-MAGIC-FREEDESKTOP]
 * <mime-type type="application/x-keyence-vk4">
 *   <comment>Keyence VK4 profilometry data</comment>
 *   <magic priority="80">
 *     <match type="string" offset="0" value="VK4_"/>
 *   </magic>
 * </mime-type>
 **/

/**
 * [FILE-MAGIC-FILEMAGIC]
 * # Keyence VK4.
 * 0 string VK4_ Keyence profilometry VK4 data
 **/

/**
 * [FILE-MAGIC-USERGUIDE]
 * Keyence profilometry VK4
 * .vk4
 * Read
 **/

/**
 *  C# Conversion 2017-09-08 16:33:03 심성현 (sim51177@gmail.com)
 *  Original C++ source is from open source SPM Application Gwyddion 2.48 (http://gwyddion.net/)
 **/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace InspPc {
    public class KeyenceHeader {
        public byte[] magic = new byte[4];
        public byte[] dll_version = new byte[4];
        public byte[] file_type = new byte[4];
    }

    public class KeyenceOffsetTable {
        public uint setting;
        public uint color_peak;
        public uint color_light;
        public uint[] light = new uint[3];
        public uint[] height = new uint[3];
        public uint color_peak_thumbnail;
        public uint color_thumbnail;
        public uint light_thumbnail;
        public uint height_thumbnail;
        public uint assemble;
        public uint line_measure;
        public uint line_thickness;
        public uint string_data;
        public uint reserved;
    }

    public class KeyenceMeasurementConditions {
        public uint size;
        public uint year;
        public uint month;
        public uint day;
        public uint hour;
        public uint minute;
        public uint second;
        public int diff_utc_by_minutes;
        public uint image_attributes;
        public uint user_interface_mode;
        public uint color_composite_mode;
        public uint num_layer;
        public uint run_mode;
        public uint peak_mode;
        public uint sharpening_level;
        public uint speed;
        public uint distance;
        public uint pitch;
        public uint optical_zoom;
        public uint num_line;
        public uint line0_pos;
        public uint[] reserved1 = new uint[3];
        public uint lens_mag;
        public uint pmt_gain_mode;
        public uint pmt_gain;
        public uint pmt_offset;
        public uint nd_filter;
        public uint reserved2;
        public uint persist_count;
        public uint shutter_speed_mode;
        public uint shutter_speed;
        public uint white_balance_mode;
        public uint white_balance_red;
        public uint white_balance_blue;
        public uint camera_gain;
        public uint plane_compensation;
        public uint xy_length_unit;
        public uint z_length_unit;
        public uint xy_decimal_place;
        public uint z_decimal_place;
        public uint x_length_per_pixel;
        public uint y_length_per_pixel;
        public uint z_length_per_digit;
        public uint[] reserved3 = new uint[5];
        public uint light_filter_type;
        public uint reserved4;
        public uint gamma_reverse;
        public uint gamma;
        public uint gamma_offset;
        public uint ccd_bw_offset;
        public uint numerical_aperture;
        public uint head_type;
        public uint pmt_gain2;
        public uint omit_color_image;
        public uint lens_id;
        public uint light_lut_mode;
        public uint light_lut_in0;
        public uint light_lut_out0;
        public uint light_lut_in1;
        public uint light_lut_out1;
        public uint light_lut_in2;
        public uint light_lut_out2;
        public uint light_lut_in3;
        public uint light_lut_out3;
        public uint light_lut_in4;
        public uint light_lut_out4;
        public uint upper_position;
        public uint lower_position;
        public uint light_effective_bit_depth;
        public uint height_effective_bit_depth;
        /* XXX: There is much more... */
    }

    public enum KeyenceFileType {
        KEYENCE_NORMAL_FILE = 0,
        KEYENCE_ASSEMBLY_FILE = 1,
        KEYENCE_ASSEMBLY_FILE_UNICODE = 2,
    }

    public class KeyenceAssemblyInformation {
        public uint size; /* The size of *all* assembly-related blocks. */
        public KeyenceFileType file_type;
        public uint stage_type;
        public uint x_position;
        public uint y_position;
    }

    public class KeyenceAssemblyConditions {
        public uint auto_adjustment;
        public uint source;
        public uint thin_out;
        public uint count_x;
        public uint count_y;
    }

    public class KeyenceAssemblyFile {
        public ushort[] source_file = new ushort[260]; /* This is Microsoft's wchar_t. */
        public uint pos_x;
        public uint pos_y;
        public uint datums_pos;
        public uint fix_distance;
        public uint distance_x;
        public uint distance_y;
    }

    public class KeyenceTrueColorImage {
        public uint width;
        public uint height;
        public uint bit_depth;
        public uint compression;
        public uint byte_size;
        public byte[] data;
    }

    public class KeyenceFalseColorImage {
        public uint width;
        public uint height;
        public uint bit_depth;
        public uint compression;
        public uint byte_size;
        public uint palette_range_min;
        public uint palette_range_max;
        public byte[] palette = new byte[0x300];
        public byte[] data;
    }

    public class KeyenceLineMeasurement {
        public uint size;
        public uint line_width;
        public byte[][] light = new byte[3][];
        public byte[][] height = new byte[3][];
    }

    public class KeyenceCharacterStrings {
        public string title;
        public string lens_name;
    }

    // main class
    public class KeyenceFile {
        public KeyenceHeader header;
        public KeyenceOffsetTable offset_table;
        public KeyenceMeasurementConditions meas_conds;
        /* The rest is optional. */
        public KeyenceAssemblyInformation assembly_info;
        public KeyenceAssemblyConditions assembly_conds;
        public uint assembly_nfiles;
        public uint nimages;
        public KeyenceAssemblyFile[] assembly_files;
        public KeyenceTrueColorImage color_peak;
        public KeyenceTrueColorImage color_light;
        public KeyenceFalseColorImage[] light = new KeyenceFalseColorImage[3];
        public KeyenceFalseColorImage[] height = new KeyenceFalseColorImage[3];
        public KeyenceLineMeasurement line_measure;
        public KeyenceCharacterStrings char_strs;
        /* Raw file contents. */
        //public byte[] buffer = null;
        //public uint size;
    }

    // main funciton
    public class KeyenceLoader {
        private static byte[] MAGIC = Encoding.ASCII.GetBytes("VK4_");
        private static byte[] MAGIC0 = Encoding.ASCII.GetBytes("\x00\x00\x00\x00");

        private const int KEYENCE_HEADER_SIZE = 12;
        private const int KEYENCE_OFFSET_TABLE_SIZE = 72;
        private const int KEYENCE_MEASUREMENT_CONDITIONS_MIN_SIZE = 304;
        private const int KEYENCE_ASSEMBLY_INFO_SIZE = 16;
        private const int KEYENCE_ASSEMBLY_CONDITIONS_SIZE = 8;
        private const int KEYENCE_ASSEMBLY_HEADERS_SIZE = (KEYENCE_ASSEMBLY_INFO_SIZE + KEYENCE_ASSEMBLY_CONDITIONS_SIZE);
        private const int KEYENCE_ASSEMBLY_FILE_SIZE = 532;
        private const int KEYENCE_TRUE_COLOR_IMAGE_MIN_SIZE = 20;
        private const int KEYENCE_FALSE_COLOR_IMAGE_MIN_SIZE = 796;
        private const int KEYENCE_LINE_MEASUREMENT_LEN = 1024;
        private const int KEYENCE_LINE_MEASUREMENT_SIZE = 18440;

        public static KeyenceFile Load(string filename) {
            byte[] buf = File.ReadAllBytes(filename);
            MemoryStream ms = new MemoryStream(buf);
            BinaryReader p = new BinaryReader(ms);

            KeyenceFile kfile = new KeyenceFile();

            kfile.header = read_header(p);
            kfile.offset_table = read_offset_table(p);
            kfile.meas_conds = read_meas_conds(p);
            read_assembly_info(kfile, p);
            read_data_images(kfile, p);
            read_color_images(kfile, p);
            read_line_meas(kfile, p);
            read_character_strs(kfile, p);

            if (kfile.nimages == 0)
                throw new Exception("ni image data");

            return kfile;
        }

        private static void CheckCanReadSize(BinaryReader p, int size) {
            if (p.BaseStream.Position + size >= p.BaseStream.Length)
                throw new Exception("Stream size not enouth to read");
        }

        private static KeyenceHeader read_header(BinaryReader p) {
            CheckCanReadSize(p, KEYENCE_HEADER_SIZE);

            KeyenceHeader header = new KeyenceHeader();

            p.Read(header.magic, 0, header.magic.Length);
            p.Read(header.dll_version, 0, header.magic.Length);
            p.Read(header.file_type, 0, header.magic.Length);

            if (header.magic.SequenceEqual(MAGIC) == false || header.file_type.SequenceEqual(MAGIC0) == false)
                throw new Exception("Invalid Header");

            return header;
        }

        private static KeyenceOffsetTable read_offset_table(BinaryReader p) {
            CheckCanReadSize(p, KEYENCE_OFFSET_TABLE_SIZE);

            KeyenceOffsetTable offsettable = new KeyenceOffsetTable();

            offsettable.setting = p.ReadUInt32();
            offsettable.color_peak = p.ReadUInt32();
            offsettable.color_light = p.ReadUInt32();

            int i;
            for (i = 0; i < offsettable.light.Length; i++)
                offsettable.light[i] = p.ReadUInt32();
            for (i = 0; i < offsettable.light.Length; i++)
                offsettable.height[i] = p.ReadUInt32();

            offsettable.color_peak_thumbnail = p.ReadUInt32();
            offsettable.color_thumbnail = p.ReadUInt32();
            offsettable.light_thumbnail = p.ReadUInt32();
            offsettable.height_thumbnail = p.ReadUInt32();
            offsettable.assemble = p.ReadUInt32();
            offsettable.line_measure = p.ReadUInt32();
            offsettable.line_thickness = p.ReadUInt32();
            offsettable.string_data = p.ReadUInt32();
            offsettable.reserved = p.ReadUInt32();

            return offsettable;
        }

        private static KeyenceMeasurementConditions read_meas_conds(BinaryReader p) {
            CheckCanReadSize(p, KEYENCE_MEASUREMENT_CONDITIONS_MIN_SIZE);

            KeyenceMeasurementConditions measconds = new KeyenceMeasurementConditions();
            measconds.size = p.ReadUInt32();
            CheckCanReadSize(p, (int)measconds.size - sizeof(uint));

            if (measconds.size < KEYENCE_MEASUREMENT_CONDITIONS_MIN_SIZE) {
                throw new Exception("MeasurementConditions::Size");
            }

            int i;
            measconds.year = p.ReadUInt32();
            measconds.month = p.ReadUInt32();
            measconds.day = p.ReadUInt32();
            measconds.hour = p.ReadUInt32();
            measconds.minute = p.ReadUInt32();
            measconds.second = p.ReadUInt32();
            measconds.diff_utc_by_minutes = p.ReadInt32();
            measconds.image_attributes = p.ReadUInt32();
            measconds.user_interface_mode = p.ReadUInt32();
            measconds.color_composite_mode = p.ReadUInt32();
            measconds.num_layer = p.ReadUInt32();
            measconds.run_mode = p.ReadUInt32();
            measconds.peak_mode = p.ReadUInt32();
            measconds.sharpening_level = p.ReadUInt32();
            measconds.speed = p.ReadUInt32();
            measconds.distance = p.ReadUInt32();
            measconds.pitch = p.ReadUInt32();
            measconds.optical_zoom = p.ReadUInt32();
            measconds.num_line = p.ReadUInt32();
            measconds.line0_pos = p.ReadUInt32();
            for (i = 0; i < measconds.reserved1.Length; i++)
                measconds.reserved1[i] = p.ReadUInt32();
            measconds.lens_mag = p.ReadUInt32();
            measconds.pmt_gain_mode = p.ReadUInt32();
            measconds.pmt_gain = p.ReadUInt32();
            measconds.pmt_offset = p.ReadUInt32();
            measconds.nd_filter = p.ReadUInt32();
            measconds.reserved2 = p.ReadUInt32();
            measconds.persist_count = p.ReadUInt32();
            measconds.shutter_speed_mode = p.ReadUInt32();
            measconds.shutter_speed = p.ReadUInt32();
            measconds.white_balance_mode = p.ReadUInt32();
            measconds.white_balance_red = p.ReadUInt32();
            measconds.white_balance_blue = p.ReadUInt32();
            measconds.camera_gain = p.ReadUInt32();
            measconds.plane_compensation = p.ReadUInt32();
            measconds.xy_length_unit = p.ReadUInt32();
            measconds.z_length_unit = p.ReadUInt32();
            measconds.xy_decimal_place = p.ReadUInt32();
            measconds.z_decimal_place = p.ReadUInt32();
            measconds.x_length_per_pixel = p.ReadUInt32();
            measconds.y_length_per_pixel = p.ReadUInt32();
            measconds.z_length_per_digit = p.ReadUInt32();
            for (i = 0; i < measconds.reserved3.Length; i++)
                measconds.reserved3[i] = p.ReadUInt32();
            measconds.light_filter_type = p.ReadUInt32();
            measconds.reserved4 = p.ReadUInt32();
            measconds.gamma_reverse = p.ReadUInt32();
            measconds.gamma = p.ReadUInt32();
            measconds.gamma_offset = p.ReadUInt32();
            measconds.ccd_bw_offset = p.ReadUInt32();
            measconds.numerical_aperture = p.ReadUInt32();
            measconds.head_type = p.ReadUInt32();
            measconds.pmt_gain2 = p.ReadUInt32();
            measconds.omit_color_image = p.ReadUInt32();
            measconds.lens_id = p.ReadUInt32();
            measconds.light_lut_mode = p.ReadUInt32();
            measconds.light_lut_in0 = p.ReadUInt32();
            measconds.light_lut_out0 = p.ReadUInt32();
            measconds.light_lut_in1 = p.ReadUInt32();
            measconds.light_lut_out1 = p.ReadUInt32();
            measconds.light_lut_in2 = p.ReadUInt32();
            measconds.light_lut_out2 = p.ReadUInt32();
            measconds.light_lut_in3 = p.ReadUInt32();
            measconds.light_lut_out3 = p.ReadUInt32();
            measconds.light_lut_in4 = p.ReadUInt32();
            measconds.light_lut_out4 = p.ReadUInt32();
            measconds.upper_position = p.ReadUInt32();
            measconds.lower_position = p.ReadUInt32();
            measconds.light_effective_bit_depth = p.ReadUInt32();
            measconds.height_effective_bit_depth = p.ReadUInt32();

            return measconds;
        }

        private static void read_assembly_info(KeyenceFile kfile, BinaryReader p) {
            uint size = (uint)p.BaseStream.Length;
            uint off = kfile.offset_table.assemble;
            uint remsize, nfiles, i, j;

            if (off == 0)
                return;

            if (size <= KEYENCE_ASSEMBLY_HEADERS_SIZE || off > size - KEYENCE_ASSEMBLY_HEADERS_SIZE) {
                throw new Exception("Stream size not enouth to read");
            }

            p.BaseStream.Position = off;

            kfile.assembly_info = new KeyenceAssemblyInformation();
            kfile.assembly_info.size = p.ReadUInt32();
            kfile.assembly_info.file_type = (KeyenceFileType)p.ReadUInt16();
            kfile.assembly_info.stage_type = p.ReadUInt16();
            kfile.assembly_info.x_position = p.ReadUInt32();
            kfile.assembly_info.y_position = p.ReadUInt32();

            kfile.assembly_conds = new KeyenceAssemblyConditions();
            kfile.assembly_conds.auto_adjustment = p.ReadByte();
            kfile.assembly_conds.source = p.ReadByte();
            kfile.assembly_conds.thin_out = p.ReadUInt16();
            kfile.assembly_conds.count_x = p.ReadUInt16();
            kfile.assembly_conds.count_y = p.ReadUInt16();

            nfiles = kfile.assembly_conds.count_x * kfile.assembly_conds.count_y;
            if (nfiles == 0)
                return;

            remsize = size - KEYENCE_ASSEMBLY_HEADERS_SIZE - off;

            if (remsize / nfiles < KEYENCE_ASSEMBLY_FILE_SIZE) {
                /* Apparently there can be large counts but no actual assembly data.
                 * I do not understand but we to not use the infomation for anything
                 * anyway. */
                kfile.assembly_conds.count_x = 0;
                kfile.assembly_conds.count_y = 0;
                kfile.assembly_nfiles = 0;
                return;
            }

            kfile.assembly_nfiles = nfiles;
            kfile.assembly_files = new KeyenceAssemblyFile[nfiles];
            for (i = 0; i < nfiles; i++) {
                kfile.assembly_files[i] = new KeyenceAssemblyFile();
                KeyenceAssemblyFile kafile = kfile.assembly_files[i];

                for (j = 0; j < kafile.source_file.Length; j++)
                    kafile.source_file[j] = p.ReadUInt16();
                kafile.pos_x = p.ReadByte();
                kafile.pos_y = p.ReadByte();
                kafile.datums_pos = p.ReadByte();
                kafile.fix_distance = p.ReadByte();
                kafile.distance_x = p.ReadUInt32();
                kafile.distance_y = p.ReadUInt32();
            }
        }

        private static bool err_DIMENSION(uint dim) {
            if (dim >= 1 && dim <= 1 << 16)
                return false;
            return true;
        }

        private static bool err_SIZE_MISMATCH(uint expected, uint real, bool strict) {
            if (expected == real || (!strict && expected < real))
                return false;
            return true;
        }

        private static void read_data_image(KeyenceFile kfile, KeyenceFalseColorImage image, uint offset, BinaryReader p) {
            uint size = (uint)p.BaseStream.Length;
            uint bps;

            if (offset == 0)
                return;

            if (size <= KEYENCE_FALSE_COLOR_IMAGE_MIN_SIZE || offset > size - KEYENCE_FALSE_COLOR_IMAGE_MIN_SIZE) {
                throw new Exception("Stream size not enouth to read");
            }

            p.BaseStream.Position = offset;

            image.width = p.ReadUInt32();
            if (err_DIMENSION(image.width))
                throw new Exception("Dimension Error");
            image.height = p.ReadUInt32();
            if (err_DIMENSION(image.height))
                throw new Exception("Dimension Error");

            image.bit_depth = p.ReadUInt32();
            if (image.bit_depth != 8 && image.bit_depth != 16 && image.bit_depth != 32) {
                throw new Exception("BPP Error");
            }

            bps = image.bit_depth / 8;

            image.compression = p.ReadUInt32();
            image.byte_size = p.ReadUInt32();
            if (err_SIZE_MISMATCH(image.width * image.height * bps, image.byte_size, true))
                throw new Exception("Size Mismatch Error");

            image.palette_range_min = p.ReadUInt32();
            image.palette_range_max = p.ReadUInt32();

            p.BaseStream.Read(image.palette, 0, image.palette.Length);

            if (size - offset - KEYENCE_FALSE_COLOR_IMAGE_MIN_SIZE < image.byte_size) {
                throw new Exception("Stream size not enouth to read");
            }
            image.data = p.ReadBytes((int)image.byte_size);
            kfile.nimages++;
        }

        private static void read_data_images(KeyenceFile kfile, BinaryReader p) {
            KeyenceOffsetTable offtable = kfile.offset_table;
            uint i;

            for (i = 0; i < kfile.light.Length; i++) {
                kfile.light[i] = new KeyenceFalseColorImage();
                read_data_image(kfile, kfile.light[i], offtable.light[i], p);
            }
            for (i = 0; i < kfile.height.Length; i++) {
                kfile.height[i] = new KeyenceFalseColorImage();
                read_data_image(kfile, kfile.height[i], offtable.height[i], p);
            }
        }

        private static void read_color_image(KeyenceFile kfile, KeyenceTrueColorImage image, uint offset, BinaryReader p) {
            uint size = (uint)p.BaseStream.Length;
            uint bps;

            if (offset == 0)
                return;

            if (size <= KEYENCE_TRUE_COLOR_IMAGE_MIN_SIZE || offset > size - KEYENCE_TRUE_COLOR_IMAGE_MIN_SIZE) {
                throw new Exception("Stream size not enouth to read");
            }

            p.BaseStream.Position = offset;

            image.width = p.ReadUInt32();
            if (err_DIMENSION(image.width))
                throw new Exception("Dimension Error");
            image.height = p.ReadUInt32();
            if (err_DIMENSION(image.height))
                throw new Exception("Dimension Error");

            image.bit_depth = p.ReadUInt32();
            if (image.bit_depth != 24) {
                throw new Exception("BPP Error");
            }
            bps = image.bit_depth / 8;

            image.compression = p.ReadUInt32();
            image.byte_size = p.ReadUInt32();
            if (err_SIZE_MISMATCH(image.width * image.height * bps, image.byte_size, true))
                throw new Exception("Size Mismatch Error");

            if (size - offset - KEYENCE_TRUE_COLOR_IMAGE_MIN_SIZE < image.byte_size) {
                throw new Exception("Stream size not enouth to read");
            }
            image.data = p.ReadBytes((int)image.byte_size);
        }

        private static void read_line_meas(KeyenceFile kfile, BinaryReader p) {
            uint size = (uint)p.BaseStream.Length;
            uint off = kfile.offset_table.line_measure;
            uint i;

            if (off == 0)
                return;

            if (size <= KEYENCE_LINE_MEASUREMENT_SIZE || off > size - KEYENCE_LINE_MEASUREMENT_SIZE) {
                throw new Exception("Stream size not enouth to read");
            }

            p.BaseStream.Position = off;

            kfile.line_measure = new KeyenceLineMeasurement();
            KeyenceLineMeasurement linemeas = kfile.line_measure;

            linemeas.size = p.ReadUInt32();
            if (size < KEYENCE_LINE_MEASUREMENT_SIZE) {
                throw new Exception("Stream size not enouth to read");
            }
            linemeas.line_width = p.ReadUInt32();
            /* XXX: We should use the real length even though the format description
             * seems to specify a fixed length.  Also note that only the first data
             * block is supposed to be used; the rest it reserved. */
            for (i = 0; i < linemeas.light.Length; i++) {
                linemeas.light[i] = p.ReadBytes(KEYENCE_LINE_MEASUREMENT_LEN * sizeof(UInt16));
            }
            for (i = 0; i < linemeas.height.Length; i++) {
                linemeas.height[i] = p.ReadBytes(KEYENCE_LINE_MEASUREMENT_LEN * sizeof(UInt32));
            }
        }

        private static void read_color_images(KeyenceFile kfile, BinaryReader p) {
            KeyenceOffsetTable offtable = kfile.offset_table;

            kfile.color_peak = new KeyenceTrueColorImage();
            read_color_image(kfile, kfile.color_peak, offtable.color_peak, p);
            kfile.color_light = new KeyenceTrueColorImage();
            read_color_image(kfile, kfile.color_light, offtable.color_light, p);
        }

        private static string read_character_str(BinaryReader p, ref uint remsize) {

            if (remsize < sizeof(UInt32)) {
                return string.Empty;
            }

            uint len = p.ReadUInt32();
            remsize -= sizeof(UInt32);

            if (len == 0)
                return string.Empty;

            if (remsize / 2 < len) {
                return string.Empty;
            }

            string s = Encoding.Unicode.GetString(p.ReadBytes((int)len * 2));

            remsize -= 2 * len;
            return s;
        }

        private static void read_character_strs(KeyenceFile kfile, BinaryReader p) {
            KeyenceCharacterStrings charstrs;
            uint remsize = (uint)p.BaseStream.Length;
            uint off = kfile.offset_table.string_data;

            if (off == 0)
                return;

            if (remsize < off) {
                throw new Exception("Stream size not enouth to read");
            }

            p.BaseStream.Position = off;
            remsize -= off;
            charstrs = kfile.char_strs = new KeyenceCharacterStrings();
            charstrs.title = read_character_str(p, ref remsize);
            charstrs.lens_name = read_character_str(p, ref remsize);
        }
    }
}