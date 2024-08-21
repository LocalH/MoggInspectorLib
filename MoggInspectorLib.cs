using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Certificates;

// MoggInspectorLib v0.1
// Use to calculate certain mogg security values, to check for PS3 errors
// Caller must provide the mogg header to the DeriveKeys method in a byte array, from byte 0 to the offset pointed to by the LITTLE-ENDIAN 32-bit integer at mogg offset 4
// Caller must then use the public variables to determine what UI to provide, and must handle keymask patching using the returned value
// The library will *not* do the patching for you, I don't want the DLL able to patch files itself

// Useful offsets for moggheads:
// 20+(_HeaderBlockSize*8) - Nonce offset, and base offset for many other values

namespace MoggInspectorLib
{
    public class Get
    {
        private readonly byte[] _Masher = new byte[32] { 0x39, 0xa2, 0xbf, 0x53, 0x7d, 0x88, 0x1d, 0x03, 0x35, 0x38, 0xa3, 0x80, 0x45, 0x24, 0xee, 0xca, 0x25, 0x6d, 0xa5, 0xc2, 0x65, 0xa9, 0x94, 0x73, 0xe5, 0x74, 0xeb, 0x54, 0xe5, 0x95, 0x3f, 0x1c };

        private readonly byte[] _CtrKey_11 = new byte[16] { 0x37, 0xb2, 0xe2, 0xb9, 0x1c, 0x74, 0xfa, 0x9e, 0x38, 0x81, 0x08, 0xea, 0x36, 0x23, 0xdb, 0xe4 };

        private readonly byte[] _HvKey_12 = new byte[16] { 0x01, 0x22, 0x00, 0x38, 0xd2, 0x01, 0x78, 0x8b, 0xdd, 0xcd, 0xd0, 0xf0, 0xfe, 0x3e, 0x24, 0x7f };
        private readonly byte[] _HvKey_14 = new byte[16] { 0x51, 0x73, 0xad, 0xe5, 0xb3, 0x99, 0xb8, 0x61, 0x58, 0x1a, 0xf9, 0xb8, 0x1e, 0xa7, 0xbe, 0xbf };
        private readonly byte[] _HvKey_15 = new byte[16] { 0xc6, 0x22, 0x94, 0x30, 0xd8, 0x3c, 0x84, 0x14, 0x08, 0x73, 0x7c, 0xf2, 0x23, 0xf6, 0xeb, 0x5a };
        private readonly byte[] _HvKey_16 = new byte[16] { 0x02, 0x1a, 0x83, 0xf3, 0x97, 0xe9, 0xd4, 0xb8, 0x06, 0x74, 0x14, 0x6b, 0x30, 0x4c, 0x00, 0x91 };
        private readonly byte[] _HvKey_17 = new byte[16] { 0x42, 0x66, 0x37, 0xb3, 0x68, 0x05, 0x9f, 0x85, 0x6e, 0x96, 0xbd, 0x1e, 0xf9, 0x0e, 0x7f, 0xbd };

        private readonly byte[] _HvKey_12_r = new byte[16] { 0xf7, 0xb6, 0xc2, 0x22, 0xb6, 0x66, 0x5b, 0xd5, 0x6c, 0xe0, 0x7d, 0x6c, 0x8a, 0x46, 0xdb, 0x18 };
        private readonly byte[] _HvKey_14_r = new byte[16] { 0x60, 0xad, 0x83, 0x0b, 0xc2, 0x2f, 0x82, 0xc5, 0xcb, 0xbf, 0xf4, 0x3d, 0x60, 0x52, 0x7e, 0x33 };
        private readonly byte[] _HvKey_15_r = new byte[16] { 0x6c, 0x68, 0x55, 0x98, 0x5b, 0x12, 0x21, 0x41, 0xe7, 0x85, 0x35, 0xca, 0x19, 0xe1, 0x9a, 0xf3 };
        private readonly byte[] _HvKey_16_r = new byte[16] { 0xa4, 0x2f, 0xf3, 0xe4, 0xe8, 0xfb, 0xa5, 0x9e, 0xac, 0x79, 0x01, 0x9e, 0xd5, 0x89, 0x66, 0xec };
        private readonly byte[] _HvKey_17_r = new byte[16] { 0x0b, 0x9c, 0x96, 0xce, 0xb6, 0xf0, 0xbc, 0xde, 0x4e, 0x9c, 0xd1, 0xc4, 0x1d, 0xeb, 0x7f, 0xe6 };

        private readonly byte[,] _HiddenKeys = new byte[12, 32]
            {
                { 0x7f, 0x95, 0x5b, 0x9d, 0x94, 0xba, 0x12, 0xf1, 0xd7, 0x5a, 0x67, 0xd9, 0x16, 0x45, 0x28, 0xdd, 0x61, 0x55, 0x55, 0xaf, 0x23, 0x91, 0xd6, 0x0a, 0x3a, 0x42, 0x81, 0x18, 0xb4, 0xf7, 0xf3, 0x04 },
                { 0x78, 0x96, 0x5d, 0x92, 0x92, 0xb0, 0x47, 0xac, 0x8f, 0x5b, 0x6d, 0xdc, 0x1c, 0x41, 0x7e, 0xda, 0x6a, 0x55, 0x53, 0xaf, 0x20, 0xc8, 0xdc, 0x0a, 0x66, 0x43, 0xdd, 0x1c, 0xb2, 0xa5, 0xa4, 0x0c },
                { 0x7e, 0x92, 0x5c, 0x93, 0x90, 0xed, 0x4a, 0xad, 0x8b, 0x07, 0x36, 0xd3, 0x10, 0x41, 0x78, 0x8f, 0x60, 0x08, 0x55, 0xa8, 0x26, 0xcf, 0xd0, 0x0f, 0x65, 0x11, 0x84, 0x45, 0xb1, 0xa0, 0xfa, 0x57 },
                { 0x79, 0x97, 0x0b, 0x90, 0x92, 0xb0, 0x44, 0xad, 0x8a, 0x0e, 0x60, 0xd9, 0x14, 0x11, 0x7e, 0x8d, 0x35, 0x5d, 0x5c, 0xfb, 0x21, 0x9c, 0xd3, 0x0e, 0x32, 0x40, 0xd1, 0x48, 0xb8, 0xa7, 0xa1, 0x0d },
                { 0x28, 0xc3, 0x5d, 0x97, 0xc1, 0xec, 0x42, 0xf1, 0xdc, 0x5d, 0x37, 0xda, 0x14, 0x47, 0x79, 0x8a, 0x32, 0x5c, 0x54, 0xf2, 0x72, 0x9d, 0xd3, 0x0d, 0x67, 0x4c, 0xd6, 0x49, 0xb4, 0xa2, 0xf3, 0x50 },
                { 0x28, 0x96, 0x5e, 0x95, 0xc5, 0xe9, 0x45, 0xad, 0x8a, 0x5d, 0x64, 0x8e, 0x17, 0x40, 0x2e, 0x87, 0x36, 0x58, 0x06, 0xfd, 0x75, 0x90, 0xd0, 0x5f, 0x3a, 0x40, 0xd4, 0x4c, 0xb0, 0xf7, 0xa7, 0x04 },
                { 0x2c, 0x96, 0x01, 0x96, 0x9b, 0xbc, 0x15, 0xa6, 0xde, 0x0e, 0x65, 0x8d, 0x17, 0x47, 0x2f, 0xdd, 0x63, 0x54, 0x55, 0xaf, 0x76, 0xca, 0x84, 0x5f, 0x62, 0x44, 0x80, 0x4a, 0xb3, 0xf4, 0xf4, 0x0c },
                { 0x7e, 0xc4, 0x0e, 0xc6, 0x9a, 0xeb, 0x43, 0xa0, 0xdb, 0x0a, 0x64, 0xdf, 0x1c, 0x42, 0x24, 0x89, 0x63, 0x5c, 0x55, 0xf3, 0x71, 0x90, 0xdc, 0x5d, 0x60, 0x40, 0xd1, 0x4d, 0xb2, 0xa3, 0xa7, 0x0d },
                { 0x2c, 0x9a, 0x0b, 0x90, 0x9a, 0xbe, 0x47, 0xa7, 0x88, 0x5a, 0x6d, 0xdf, 0x13, 0x1d, 0x2e, 0x8b, 0x60, 0x5e, 0x55, 0xf2, 0x74, 0x9c, 0xd7, 0x0e, 0x60, 0x40, 0x80, 0x1c, 0xb7, 0xa1, 0xf4, 0x02 },
                { 0x28, 0x96, 0x5b, 0x95, 0xc1, 0xe9, 0x40, 0xa3, 0x8f, 0x0c, 0x32, 0xdf, 0x43, 0x1d, 0x24, 0x8d, 0x61, 0x09, 0x54, 0xab, 0x27, 0x9a, 0xd3, 0x58, 0x60, 0x16, 0x84, 0x4f, 0xb3, 0xa4, 0xf3, 0x0d },
                { 0x25, 0x93, 0x08, 0xc0, 0x9a, 0xbd, 0x10, 0xa2, 0xd6, 0x09, 0x60, 0x8f, 0x11, 0x1d, 0x7a, 0x8f, 0x63, 0x0b, 0x5d, 0xf2, 0x21, 0xec, 0xd7, 0x08, 0x62, 0x40, 0x84, 0x49, 0xb0, 0xad, 0xf2, 0x07 },
                { 0x29, 0xc3, 0x0c, 0x96, 0x96, 0xeb, 0x10, 0xa0, 0xda, 0x59, 0x32, 0xd3, 0x17, 0x41, 0x25, 0xdc, 0x63, 0x08, 0x04, 0xae, 0x77, 0xcb, 0x84, 0x5a, 0x60, 0x4d, 0xdd, 0x45, 0xb5, 0xf4, 0xa0, 0x05 }
            };
        private readonly byte[,] _HiddenKeys17_1 = new byte[12, 32]
            {
                { 0x4c, 0x22, 0xd9, 0x28, 0xa6, 0x23, 0x01, 0x62, 0x0a, 0x84, 0x86, 0x27, 0xbb, 0xcc, 0x88, 0x9e, 0x33, 0x3a, 0x6b, 0x23, 0x92, 0x22, 0xa2, 0xb4, 0x81, 0x64, 0x4e, 0x8d, 0x25, 0x69, 0x9f, 0xdc },
                { 0x64, 0xf1, 0x5f, 0x54, 0xca, 0x70, 0xb8, 0x8b, 0xf8, 0xaa, 0x2a, 0xd3, 0xd9, 0xec, 0x3b, 0x49, 0xe8, 0x0a, 0x3e, 0xe3, 0x46, 0xb1, 0xbf, 0x27, 0x1b, 0x6c, 0x76, 0x11, 0xc8, 0x35, 0x7a, 0xb4 },
                { 0x74, 0xf7, 0x42, 0xa5, 0xf1, 0xc7, 0x56, 0x2d, 0x31, 0xe1, 0x73, 0xf9, 0x96, 0x93, 0x89, 0x85, 0xa7, 0xac, 0x34, 0x46, 0x68, 0xd0, 0xbd, 0x6e, 0x08, 0xff, 0x5e, 0x8a, 0xae, 0x93, 0xa2, 0xdb },
                { 0xf8, 0xa3, 0x21, 0x5c, 0xc2, 0xbf, 0xc1, 0xc0, 0xaf, 0x79, 0x1d, 0x96, 0x43, 0x43, 0xd5, 0xf9, 0x8f, 0xd9, 0xc8, 0xc9, 0xce, 0x6e, 0x68, 0x93, 0x32, 0x5c, 0x80, 0xfa, 0x18, 0xe4, 0x3a, 0x06 },
                { 0x8d, 0x99, 0x57, 0xb0, 0x0d, 0xe0, 0x26, 0xdc, 0xda, 0xd3, 0xda, 0x2b, 0x03, 0x74, 0x35, 0xc3, 0xfa, 0x23, 0x4e, 0x96, 0x62, 0xea, 0xf0, 0xd4, 0xc6, 0xc7, 0x7f, 0x6e, 0xba, 0xa9, 0x42, 0x7d },
                { 0xb1, 0x70, 0x75, 0x8c, 0x92, 0x76, 0xb6, 0x3c, 0xfb, 0x72, 0x78, 0x7c, 0x19, 0x5e, 0x31, 0xa5, 0x0c, 0x6a, 0x1e, 0x24, 0x79, 0x51, 0x85, 0xa0, 0x53, 0xe4, 0x3e, 0xc2, 0x86, 0x15, 0x25, 0xba },
                { 0x19, 0xb1, 0xbc, 0x30, 0x61, 0x7e, 0x84, 0x06, 0x34, 0xb9, 0x81, 0xa9, 0x5d, 0xd3, 0x4c, 0x86, 0x2b, 0xb1, 0xd4, 0xa9, 0xf0, 0x21, 0xfb, 0x61, 0xfe, 0x8b, 0x26, 0x83, 0x92, 0x20, 0xe6, 0xbc },
                { 0x49, 0x1a, 0xbd, 0xc3, 0xdb, 0x75, 0x30, 0x22, 0x84, 0x11, 0xc8, 0x1c, 0x33, 0xe8, 0x4d, 0x5a, 0x34, 0x79, 0xc3, 0x9f, 0xed, 0x8f, 0x81, 0xf6, 0xb3, 0xa5, 0xe8, 0xe1, 0x04, 0xee, 0x3a, 0xf0 },
                { 0x44, 0xb1, 0x0a, 0x9f, 0x80, 0x9a, 0xb0, 0x20, 0x4c, 0x16, 0xc7, 0x9c, 0xc9, 0x78, 0x84, 0xa9, 0x92, 0xc7, 0xea, 0x53, 0x81, 0x4e, 0xc3, 0xcc, 0x2f, 0x0b, 0x0c, 0x86, 0xe0, 0x8d, 0xa5, 0x02 },
                { 0xdf, 0x64, 0x2a, 0x87, 0xcb, 0xa7, 0x22, 0xd5, 0xff, 0x9c, 0x8d, 0x58, 0xc9, 0x89, 0x35, 0x38, 0x79, 0xa4, 0x09, 0xc8, 0x2e, 0xe8, 0xb5, 0x90, 0x8a, 0xe9, 0xd3, 0xa3, 0x2d, 0x49, 0x71, 0x9c },
                { 0x04, 0xec, 0xc2, 0x82, 0x0e, 0x61, 0xab, 0xb3, 0x4b, 0x4c, 0x6c, 0x10, 0xe5, 0xfa, 0x8f, 0xc7, 0xdd, 0xa5, 0x45, 0x16, 0x5c, 0x37, 0xcf, 0x70, 0xe9, 0xfe, 0x5d, 0x9b, 0xe6, 0xb2, 0xa5, 0x85 },
                { 0xb3, 0xcc, 0x1c, 0xaa, 0x9a, 0x16, 0x32, 0xe7, 0x0c, 0x41, 0xc0, 0xbd, 0x70, 0x1e, 0xbc, 0x72, 0x17, 0xcb, 0x04, 0x6b, 0x14, 0x00, 0x13, 0xb6, 0x37, 0x33, 0xa3, 0xb7, 0xd3, 0xdd, 0xc9, 0x1a }
            };
        private readonly byte[,] _HiddenKeys17_4 = new byte[12, 32]
            {
                { 0x53, 0xb6, 0x2e, 0xf4, 0xe7, 0xec, 0x46, 0x0a, 0xd2, 0xa7, 0x9a, 0xb7, 0x6f, 0x00, 0xb6, 0xe8, 0x04, 0x6d, 0x28, 0xd0, 0xf3, 0xaf, 0xa6, 0x5d, 0xe5, 0x27, 0xb9, 0x06, 0xb6, 0x69, 0xa2, 0xd6 },
                { 0x1b, 0xf1, 0x33, 0x88, 0xc6, 0xce, 0x99, 0xf8, 0x72, 0x3a, 0x39, 0x94, 0xdc, 0x59, 0x74, 0x9c, 0x41, 0x91, 0x65, 0xc9, 0x55, 0xd6, 0x4c, 0xa6, 0x52, 0x05, 0xd7, 0xab, 0xe9, 0xda, 0x3d, 0x5c },
                { 0xda, 0x56, 0x1b, 0xb6, 0x2b, 0xc1, 0x22, 0x91, 0x06, 0xb2, 0xa6, 0x5c, 0xbc, 0x4f, 0x50, 0x4b, 0x3d, 0x6a, 0x11, 0xcd, 0xca, 0xea, 0xab, 0x5b, 0x69, 0x8c, 0xbf, 0x93, 0xd3, 0xf7, 0x55, 0xe6 },
                { 0x73, 0x92, 0xc9, 0xd9, 0xe3, 0x52, 0x5d, 0x56, 0x74, 0x73, 0xf8, 0xaa, 0xcf, 0xcb, 0xef, 0x5d, 0xe9, 0xc8, 0x97, 0x96, 0xdc, 0x7e, 0xc7, 0xf7, 0xd4, 0x83, 0x9b, 0x9d, 0x90, 0x06, 0xb5, 0x60 },
                { 0x77, 0x99, 0xa9, 0x0f, 0x83, 0x9b, 0x1a, 0xdd, 0xbc, 0x60, 0x53, 0xee, 0xf4, 0xfa, 0x77, 0x96, 0xd0, 0x0f, 0x8f, 0x4b, 0xbb, 0x2e, 0x83, 0xf5, 0x19, 0x27, 0xc2, 0xa8, 0x10, 0x40, 0xf0, 0xf3 },
                { 0xaa, 0xe1, 0x9d, 0xf1, 0x60, 0x38, 0xf9, 0xe1, 0x34, 0x10, 0xa7, 0x85, 0xe3, 0x9a, 0x77, 0xc7, 0x11, 0x9c, 0xeb, 0x71, 0x71, 0xc1, 0x2b, 0x0e, 0x95, 0x2e, 0x0c, 0xa7, 0x94, 0x69, 0x0b, 0x56 },
                { 0x86, 0x62, 0xf2, 0x77, 0xd0, 0x33, 0x90, 0x58, 0xf8, 0x22, 0xe3, 0xdd, 0x48, 0xb4, 0x98, 0xfe, 0x9e, 0xdf, 0x47, 0x72, 0xa8, 0x38, 0x15, 0x3d, 0x8b, 0x11, 0xe3, 0xdd, 0xff, 0xf9, 0x54, 0x9d },
                { 0xa3, 0x2e, 0xe6, 0x54, 0x34, 0x94, 0x8f, 0x3d, 0x6c, 0x78, 0xc0, 0x06, 0x28, 0xe9, 0x84, 0x5a, 0x80, 0xb8, 0xbe, 0xbb, 0x03, 0xb1, 0x1b, 0xb6, 0xdc, 0xb6, 0x4c, 0xd5, 0xe2, 0xbf, 0x78, 0x2f },
                { 0x35, 0x81, 0x86, 0xc9, 0x42, 0xcb, 0x1b, 0x2b, 0x87, 0x32, 0xae, 0x98, 0x73, 0x8e, 0xce, 0x02, 0xa7, 0x88, 0x2c, 0xbe, 0xfa, 0x54, 0x9d, 0x84, 0xbe, 0xc4, 0x0b, 0xff, 0xe6, 0xd9, 0x18, 0x2e },
                { 0xca, 0x53, 0xb0, 0x5f, 0x14, 0x3a, 0x40, 0xb2, 0x5f, 0x8d, 0x5e, 0x10, 0x86, 0x0d, 0x63, 0xbd, 0xc7, 0x4b, 0x71, 0xd6, 0xff, 0xdd, 0x2d, 0x1f, 0xd9, 0x06, 0x20, 0xf6, 0xf8, 0x2f, 0x7d, 0x56 },
                { 0x40, 0x2f, 0x93, 0x66, 0x9b, 0xee, 0x29, 0x5c, 0x91, 0xcf, 0xa6, 0xad, 0x47, 0x63, 0x01, 0x87, 0x51, 0x6c, 0xe8, 0x29, 0x55, 0x68, 0x5e, 0x11, 0xc2, 0x48, 0x23, 0x96, 0x05, 0x78, 0xb3, 0xa1 },
                { 0x8f, 0xfb, 0x7e, 0xad, 0x69, 0x6a, 0x24, 0xcd, 0x03, 0x97, 0xca, 0xb8, 0x48, 0x39, 0xf6, 0xdd, 0x56, 0x80, 0x61, 0xe7, 0x66, 0xee, 0x5c, 0x55, 0xd1, 0x52, 0x57, 0xce, 0xd2, 0xc0, 0xbe, 0xc1 }
            };
        private readonly byte[,] _HiddenKeys17_6 = new byte[12, 32]
            {
                { 0x35, 0xb3, 0xda, 0x45, 0x95, 0xd2, 0x5c, 0x4e, 0x65, 0x01, 0x5f, 0x84, 0x61, 0x61, 0x6a, 0x08, 0xb0, 0x0d, 0x41, 0xd3, 0xa7, 0xf4, 0xb8, 0xa1, 0x78, 0x08, 0xe2, 0x75, 0x29, 0x1e, 0xfe, 0x8d },
                { 0x18, 0x9a, 0x4c, 0x81, 0x2e, 0x8a, 0x6d, 0x40, 0x17, 0xec, 0x55, 0x1b, 0x4b, 0x39, 0x28, 0x84, 0x63, 0x69, 0xc3, 0x6b, 0x24, 0x30, 0x71, 0x00, 0xcd, 0x0e, 0xdd, 0xda, 0xa1, 0xfa, 0x1b, 0xb9 },
                { 0x41, 0xc7, 0x6e, 0xe3, 0x6d, 0xda, 0xb1, 0x96, 0x7c, 0x19, 0x0f, 0x98, 0x6e, 0x12, 0xb3, 0x41, 0x99, 0x0f, 0xd5, 0x4c, 0x32, 0x7e, 0x9f, 0xba, 0x0b, 0x5f, 0xe7, 0xa1, 0x5b, 0x73, 0x59, 0x8b },
                { 0xff, 0x37, 0xa5, 0x37, 0x8a, 0xf7, 0x8d, 0xa8, 0xf1, 0x21, 0xfe, 0xfb, 0xc1, 0x08, 0x2f, 0x30, 0x84, 0xc2, 0x4f, 0x6c, 0x00, 0x32, 0x9f, 0xa7, 0xcb, 0x7f, 0xb8, 0x15, 0x51, 0x4f, 0xd7, 0xeb },
                { 0x29, 0x5b, 0xaa, 0x6a, 0x41, 0xca, 0xc8, 0xff, 0xbf, 0x9b, 0x4e, 0x0f, 0xcc, 0x29, 0xc6, 0x92, 0x15, 0x8e, 0xec, 0x97, 0x60, 0xc7, 0xa9, 0x68, 0x40, 0x61, 0x89, 0x29, 0x8e, 0x5a, 0x05, 0x50 },
                { 0x4e, 0x08, 0x6a, 0x65, 0x42, 0x6e, 0x89, 0x63, 0xf1, 0xc3, 0x45, 0x06, 0xb0, 0x52, 0xe9, 0xba, 0x9e, 0xec, 0x6f, 0x9a, 0x99, 0x4d, 0x07, 0xe7, 0x8a, 0x1b, 0x03, 0x2f, 0xd1, 0x07, 0xe7, 0xd4 },
                { 0x57, 0x12, 0x80, 0xf2, 0x74, 0x43, 0x60, 0x68, 0x17, 0xac, 0x2f, 0xca, 0x55, 0x2b, 0x0d, 0x36, 0x16, 0xb8, 0xd6, 0x45, 0xe3, 0xd8, 0x4c, 0x8f, 0xd7, 0x8d, 0x25, 0xeb, 0x4a, 0x2b, 0x07, 0xd5 },
                { 0x8c, 0xdf, 0xb8, 0xa6, 0x1e, 0x94, 0x4f, 0x9a, 0x10, 0x80, 0x67, 0xe2, 0x0d, 0x61, 0xbb, 0xa7, 0x54, 0x83, 0xac, 0x2e, 0xfa, 0xda, 0xee, 0xd4, 0xc4, 0x5a, 0x77, 0xce, 0xae, 0x03, 0x17, 0xb6 },
                { 0x44, 0x34, 0x3f, 0xa8, 0x66, 0x5d, 0x85, 0x17, 0xc1, 0xda, 0x8d, 0x26, 0xb3, 0x33, 0xba, 0x87, 0x57, 0x10, 0x6c, 0xb9, 0x7e, 0x43, 0xcb, 0x97, 0xfd, 0x2e, 0x48, 0xdc, 0x3d, 0xa4, 0xbf, 0x8a },
                { 0xbb, 0x9a, 0x0e, 0x29, 0x7d, 0x8d, 0x17, 0x46, 0x08, 0x61, 0x8e, 0x72, 0xab, 0xef, 0x4b, 0x40, 0xc4, 0x93, 0x24, 0x03, 0x21, 0x54, 0x02, 0x97, 0xb5, 0x12, 0xab, 0x42, 0x4f, 0x23, 0x2a, 0x6f },
                { 0x7b, 0xd5, 0x0c, 0x35, 0xe3, 0x62, 0xe4, 0x3b, 0xee, 0x23, 0x30, 0x9e, 0x61, 0x70, 0xbe, 0xbf, 0x8f, 0xa7, 0x4b, 0xed, 0x97, 0x3b, 0xd1, 0xcb, 0xdd, 0xd2, 0x0b, 0xe5, 0xe1, 0xb9, 0xe6, 0x52 },
                { 0x69, 0xa9, 0x4b, 0x0f, 0x1c, 0x58, 0xcb, 0x77, 0xe2, 0x12, 0xea, 0x94, 0xdf, 0x47, 0x3f, 0x53, 0x26, 0xba, 0x0e, 0x6e, 0x09, 0xc3, 0xb2, 0x22, 0x68, 0xdd, 0x4c, 0x5c, 0xfd, 0x66, 0x86, 0x73 }
            };
        private readonly byte[,] _HiddenKeys17_8 = new byte[12, 32]
            {
                { 0x9e, 0xdf, 0xa5, 0xbb, 0x02, 0xca, 0x0c, 0x2b, 0x51, 0x02, 0x1a, 0x35, 0x11, 0x62, 0x8a, 0x0f, 0x66, 0x31, 0x6e, 0x73, 0x0a, 0x68, 0x5f, 0x55, 0xe0, 0x51, 0x4f, 0x73, 0x50, 0x53, 0xb4, 0x9c },
                { 0x98, 0x3a, 0xfa, 0x87, 0x4c, 0x44, 0x70, 0xa8, 0x15, 0xe4, 0x5a, 0x85, 0x73, 0xae, 0x1a, 0x32, 0x26, 0x63, 0x28, 0x11, 0x4d, 0x80, 0x73, 0xab, 0x3d, 0x86, 0x9c, 0x03, 0x99, 0xac, 0x10, 0x1a },
                { 0xa4, 0xb6, 0xa4, 0xfc, 0x5a, 0xec, 0x7a, 0x18, 0xc0, 0x2c, 0x79, 0x74, 0xe2, 0xdb, 0x35, 0x14, 0x02, 0xfe, 0x91, 0x0e, 0x13, 0xa9, 0x44, 0xdf, 0x94, 0x85, 0x3f, 0x9a, 0x41, 0xcb, 0x34, 0x32 },
                { 0x7b, 0x87, 0xc0, 0xf6, 0xae, 0xf6, 0x44, 0x10, 0xd2, 0x01, 0xaf, 0x18, 0x67, 0x98, 0xc2, 0x0e, 0xec, 0x9a, 0x41, 0x42, 0xea, 0x90, 0xef, 0xde, 0xd6, 0xbf, 0x12, 0x6c, 0x8b, 0x2b, 0x6e, 0x13 },
                { 0x63, 0xe9, 0xb0, 0x24, 0xd2, 0x0f, 0xc1, 0x3c, 0x6f, 0x60, 0xec, 0xd6, 0xce, 0x9a, 0xcc, 0x7d, 0x25, 0x04, 0x95, 0x81, 0x9d, 0xb9, 0x9a, 0xf1, 0x8b, 0x82, 0x1f, 0xf9, 0xa3, 0xa6, 0x2b, 0x3a },
                { 0xc1, 0x5d, 0xa1, 0xd2, 0x49, 0x92, 0x02, 0x8d, 0x76, 0x7a, 0x32, 0x76, 0xb7, 0xfd, 0x64, 0xcb, 0x51, 0x2d, 0x51, 0xc7, 0xfc, 0x0e, 0x2f, 0xa4, 0xf8, 0x1d, 0xf1, 0x02, 0x81, 0x88, 0x49, 0x4f },
                { 0x0a, 0xfc, 0xcb, 0x82, 0x34, 0xad, 0x23, 0xdb, 0x13, 0x1b, 0x4b, 0x7a, 0xa4, 0xd6, 0x26, 0xfa, 0xdf, 0x86, 0x65, 0x64, 0xb0, 0x6f, 0x95, 0x84, 0x92, 0xd0, 0x4d, 0x31, 0x68, 0x61, 0x56, 0x21 },
                { 0xdf, 0x60, 0xee, 0xdb, 0xc5, 0x55, 0x26, 0xc0, 0x0e, 0x3f, 0xa8, 0x4b, 0xd4, 0xb1, 0x54, 0x3f, 0x60, 0x93, 0xbf, 0xb3, 0x8a, 0x46, 0x79, 0x34, 0x36, 0xa9, 0x16, 0x9d, 0x20, 0xf7, 0xd3, 0x61 },
                { 0x92, 0x63, 0x1e, 0x54, 0xe4, 0xdf, 0x9b, 0x42, 0x32, 0xb4, 0xa8, 0x3d, 0x2e, 0x48, 0x3a, 0x96, 0x89, 0x0f, 0xcf, 0xaa, 0x22, 0x09, 0x1d, 0xd3, 0xf9, 0x28, 0x25, 0xce, 0x67, 0x57, 0xd6, 0xd0 },
                { 0xc1, 0x30, 0x1d, 0x91, 0xa1, 0xb7, 0x39, 0x1e, 0xe4, 0xd9, 0x08, 0x88, 0xcd, 0x19, 0x88, 0x09, 0xfc, 0xc1, 0x38, 0x59, 0x7c, 0x4b, 0xd7, 0xd9, 0xf5, 0x10, 0xa3, 0x9c, 0x1e, 0x5e, 0xf1, 0x30 },
                { 0x36, 0x00, 0x3f, 0x13, 0xa0, 0x7a, 0xb6, 0x02, 0x86, 0x4d, 0xc2, 0x70, 0x19, 0x1f, 0xd1, 0xd9, 0x8e, 0x0b, 0x64, 0x4a, 0xf2, 0xc6, 0xeb, 0xb5, 0x1c, 0x14, 0x6c, 0xc0, 0x54, 0xd3, 0x69, 0x5c },
                { 0x00, 0xb1, 0xa8, 0x7f, 0xa2, 0x91, 0xad, 0x8e, 0x08, 0xf6, 0xc9, 0x03, 0x71, 0xa9, 0x74, 0x64, 0x66, 0xde, 0x4e, 0x02, 0x08, 0x35, 0x39, 0x40, 0x9c, 0x75, 0x10, 0x0d, 0x9d, 0x61, 0x7f, 0x63 }
            };
        private readonly byte[,] _HiddenKeys17_10 = new byte[12, 32]
            {
                { 0xfe, 0x0e, 0x46, 0xa5, 0x59, 0x14, 0x7c, 0x30, 0xb4, 0x6a, 0x42, 0xcb, 0x75, 0x71, 0xbb, 0xcd, 0xd8, 0xc3, 0x20, 0xdc, 0x2e, 0xf7, 0x02, 0x8b, 0x03, 0x36, 0x43, 0x96, 0xaf, 0xde, 0x2d, 0x71 },
                { 0xaf, 0xa3, 0xf3, 0x3b, 0xdb, 0x8f, 0xe2, 0xf5, 0x96, 0x45, 0x8a, 0x37, 0xed, 0xb9, 0xab, 0x18, 0x1f, 0xb2, 0xdd, 0x75, 0xa6, 0x2a, 0x66, 0xe6, 0xc4, 0xc1, 0x44, 0xf4, 0x78, 0x15, 0x9f, 0x38 },
                { 0xe9, 0x61, 0x9c, 0x1c, 0x51, 0x16, 0x49, 0x77, 0xb3, 0xe3, 0xc5, 0xf9, 0x57, 0x73, 0x78, 0xee, 0x72, 0xa5, 0x11, 0x24, 0x0e, 0xd6, 0x81, 0x85, 0xf1, 0xb7, 0xd7, 0x09, 0x0a, 0x95, 0x04, 0x82 },
                { 0xb5, 0x82, 0x8b, 0xc7, 0x2b, 0x0b, 0xe8, 0x45, 0x23, 0x5a, 0xe7, 0xb4, 0xe4, 0x32, 0x59, 0x82, 0xb0, 0x89, 0x2f, 0xc8, 0x0f, 0x53, 0xbd, 0x1c, 0xda, 0x9b, 0x8e, 0x28, 0x6f, 0x0f, 0x7e, 0xf0 },
                { 0x54, 0x1d, 0x9e, 0xbc, 0x51, 0xdf, 0x27, 0x95, 0xa4, 0x3f, 0xcc, 0xcb, 0xb4, 0x1c, 0x3d, 0x60, 0x15, 0xef, 0x5d, 0x3e, 0x46, 0x3d, 0x2b, 0x17, 0x98, 0x97, 0x89, 0xa0, 0x7f, 0xf1, 0x59, 0xa3 },
                { 0xf2, 0xe9, 0xb4, 0x72, 0xf2, 0x65, 0x22, 0xa3, 0x38, 0x1a, 0xdd, 0xe3, 0x83, 0xed, 0x95, 0xd1, 0x6e, 0xcf, 0xc6, 0xeb, 0x87, 0x63, 0x4f, 0x71, 0x85, 0xa9, 0x15, 0x62, 0x43, 0x6c, 0x18, 0x98 },
                { 0x25, 0x8b, 0xfa, 0xf6, 0xfc, 0x92, 0x38, 0x9e, 0xbf, 0x53, 0x45, 0x33, 0xab, 0x9c, 0xcd, 0x53, 0x41, 0x79, 0xc3, 0x27, 0x50, 0xbc, 0xd2, 0x47, 0x3a, 0x49, 0x39, 0xf9, 0x87, 0x54, 0x8f, 0xfe },
                { 0x29, 0x5a, 0xea, 0xba, 0x0a, 0xef, 0x1f, 0xcd, 0x22, 0x1e, 0x48, 0x3e, 0x70, 0xf0, 0x62, 0x21, 0x8c, 0x83, 0xf6, 0x8a, 0x10, 0x3b, 0x55, 0x6e, 0xb5, 0x35, 0xbb, 0x70, 0x4f, 0xec, 0xa1, 0xfb },
                { 0x08, 0x2c, 0x3a, 0xec, 0x3f, 0xfa, 0x71, 0xb7, 0x25, 0x3c, 0x4b, 0xfc, 0xe5, 0x5c, 0xaf, 0x6b, 0x31, 0x43, 0x05, 0x73, 0x99, 0xb3, 0x56, 0xf7, 0xcd, 0xe5, 0x44, 0x81, 0x81, 0x97, 0xba, 0xd9 },
                { 0x03, 0x4d, 0xd2, 0xf2, 0x44, 0xb6, 0x8f, 0xa2, 0x94, 0xfd, 0x8d, 0x0b, 0x22, 0x97, 0x91, 0x50, 0xb4, 0xaf, 0x5a, 0xd2, 0x92, 0x94, 0x6b, 0xa3, 0x55, 0x56, 0xa8, 0xe5, 0x3f, 0x5c, 0xdd, 0x4f },
                { 0x81, 0x84, 0x19, 0x91, 0x45, 0x40, 0x3f, 0x9d, 0x7c, 0x47, 0xf4, 0x5d, 0x57, 0x56, 0x80, 0x30, 0xd9, 0x98, 0x1c, 0x65, 0x5e, 0x07, 0xce, 0x9d, 0xd1, 0x20, 0x62, 0x9d, 0x45, 0x8f, 0xbb, 0x0c },
                { 0xb5, 0xa2, 0x15, 0x9d, 0x15, 0x86, 0x9f, 0x6e, 0x80, 0x55, 0x8c, 0xe6, 0x6c, 0x68, 0x71, 0xee, 0x7e, 0xed, 0x19, 0x9c, 0xb0, 0x80, 0xc5, 0x5f, 0xdc, 0x9f, 0xd1, 0x4a, 0x01, 0x36, 0xf4, 0x39 }
        };

        // These are here more for documentation than anything as the method will run the derivations regardless, but will enable detection of these moggs so the caller can display appropriate UI
        private readonly byte[] _C3v12BadPs3Mask = new byte[16] { 0x6c, 0x6c, 0x65, 0x63, 0x74, 0x69, 0x76, 0x65, 0x2d, 0x74, 0x6f, 0x6f, 0x6c, 0x73, 0x2d, 0x62 };
        private readonly byte[] _C3v13BadPs3Mask = new byte[16] { 0xc3, 0xc3, 0xc3, 0xc3, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b };
        private readonly byte[] _C3v12FixedPs3Mask = new byte[16] { 0xf1, 0xb4, 0xb8, 0xb0, 0x48, 0xaf, 0xcb, 0x9b, 0x4b, 0x53, 0xe0, 0x56, 0x64, 0x57, 0x68, 0x39 };
        private readonly byte[] _C3v13FixedPs3Mask = new byte[16] { 0xa5, 0xce, 0xfd, 0x06, 0x11, 0x93, 0x23, 0x21, 0xf8, 0x87, 0x85, 0xea, 0x95, 0xe4, 0x94, 0xd4 };
        private readonly byte[] _C3v11Nonce = new byte[16] { 0x00, 0x00, 0x00, 0x00, 0x63, 0x33, 0x2d, 0x63, 0x75, 0x73, 0x74, 0x6F, 0x6D, 0x73, 0x31, 0x34 };

        public uint _Version; // read from bytes 0-3 of the header
        private uint _OggOffset; // read from bytes 4-7 of the header
        private uint _HeaderBlockSize; // read from bytes 16-19 of the header
        private uint _NonceOffset; // calculated from 20+(_HeaderBlockSize*8)
        private uint _MagicAOffset; // calculated from _NonceOffset+16
        private uint _MagicBOffset; // calculated from _NonceOffset+24
        private uint _Ps3MaskOffset; // calculated from _NonceOffset+32
        private uint _XboxMaskOffset; // calculated from _NonceOffset+48
        private uint _KeyIndexOffset; // calculated from _NonceOffset+64 if v12-v16, _NonceOffset+72 if v17
        private uint _V17KeysetOffset; // calculated from _NonceOffset+64
        public uint _V17Keyset; // read from the eight bytes at _V17KeysetOffset, *only* if _Version is 17

        public ulong _MagicA; // this is used to xor a specific offset in the decrypted audio as a form of obfuscation
        public ulong _MagicB; // this is used to xor a different specific offset in the decrypted audio
        public uint _Ps3Index; // this is the key index into the _HiddenKeys array as read from the header, HMX games always take the stored value and modulo 6
        public uint _XboxIndex; // this is _Ps3Index + 6
        public byte[] _XboxAesKey = new byte[16]; // this will hold the final derived xbox key
        public byte[] _Ps3AesKey = new byte[16]; // this will hold the final derived ps3 key
        public byte[] _Nonce = new byte[16]; // normally used as read, this is the IV for the AES CTR encryption so we just document it
        public byte[] _Ps3Mask = new byte[16]; // used directly, no encryption
        public byte[] _XboxMask = new byte[16]; // as read from the header, this is encrypted with the appropriate _HvKey
        public byte[] _Ps3GrindArrayResult = new byte[16]; // this is kept so we can xor it with the xbox key to get the correct ps3 keymask
        public byte[] _Ps3FixedMask = new byte[16]; // used only when _XboxAesKey and _Ps3AesKey don't match, is _XboxAesKey XOR _Ps3GrindArrayResult
        public bool _KeymaskMismatch = false; // this will be set to true if the keys don }t match, so the caller knows a patch is needed
        public bool _IsC3Mogg = false; // this will be set to true if the appropriate conditions are met for a mogg to be of C3 origin

        private int Roll(int x)
        {
            return ((x + 0x13) % 0x20);
        }

        private void Swap(ref byte b1, ref byte b2)
        {
            byte tmp = b1;
            b1 = b2;
            b2 = tmp;
        }
        private byte[] Shuffle1(byte[] key)
        {
            for (int i = 0; i < 8; i++)
            {
                int o = Roll(i << 2);
                Swap(ref key[o], ref key[(i * 4) + 2]);
                o = Roll((i * 4) + 3);
                Swap(ref key[o], ref key[(i * 4) + 1]);
            }
            return key;
        }
        private byte[] Shuffle2(byte[] key)
        {
            for (int i = 0; i < 8; i++)
            {
                Swap(ref key[((7 - i) * 4) + 1], ref key[(i * 4) + 2]);
                Swap(ref key[(7 - i) * 4], ref key[(i * 4) + 3]);
            }
            return key;
        }
        private byte[] Shuffle3(byte[] key)
        {
            for (int i = 0; i < 8; i++)
            {
                int o = Roll(((7 - i) * 4) + 1);
                Swap(ref key[o], ref key[(i * 4) + 2]);
                Swap(ref key[(7 - i) * 4], ref key[(i * 4) + 3]);
            }
            return key;
        }
        private byte[] Shuffle4(byte[] key)
        {
            for (int i = 0; i < 8; i++)
            {
                Swap(ref key[((7 - i) * 4) + 1], ref key[(i * 4) + 2]);
                int o = Roll((7 - i) * 4);
                Swap(ref key[o], ref key[(i * 4) + 3]);
            }
            return key;
        }
        private byte[] Shuffle5(byte[] key)
        {
            for (int i = 0; i < 8; i++)
            {
                int o = Roll((i * 4) + 2);
                Swap(ref key[((7 - i) * 4) + 1], ref key[o]);
                Swap(ref key[(7 - i) * 4], ref key[(i * 4) + 3]);
            }
            return key;
        }
        private byte[] Shuffle6(byte[] key)
        {
            for (int i = 0; i < 8; i++)
            {
                Swap(ref key[((7 - i) * 4) + 1], ref key[(i * 4) + 2]);
                int o = Roll((i * 4) + 3);
                Swap(ref key[(7 - i) * 4], ref key[o]);
            }
            return key;
        }
        private byte[] Supershuffle(byte[] key)
        {
            key = Shuffle1(key);
            key = Shuffle2(key);
            key = Shuffle3(key);
            key = Shuffle4(key);
            key = Shuffle5(key);
            key = Shuffle6(key);
            return key;
        }
        private byte[] Mash(byte[] key)
        {
            for (int i = 0; i < 32; i++)
            {
                key[i] = (byte)(key[i] ^ _Masher[i]);
            }
            return key;
        }
        private byte[] RevealKey(byte[] key)
        {
            for (int i = 0; i < 14; i++)
            { 
                key = Supershuffle(key);
            }
            key = Mash(key);
            return key;
        }
        public void DeriveKeys(byte[] header)
        {

        }
    }
}
