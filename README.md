## MoggInspectorLib


This library will run the key derivation process for both PS3 and 360/others in order to double check that the PS3 keymask is appropriately set to generate a matching AES key.

This library will **not** decrypt any MOGGs.

This library is **not** compatible with encrypted Beatles MOGGs, do not patch them with it or you will corrupt them.

Developed with .NET Framework 4.8.1 and .NET Standard 2.0.

# Usage

Include the library as a reference in your project.

`DeriveKeys(byte[] header, bool red)`

`header` is a byte array containing the MOGG header. The size of this header is the *little-endian* UInt from offset 4 in the MOGG file.

`red` indicates whether to use the red HVKeys that have been found in various builds of the games. We have never found a MOGG that uses them, so this code path is untested. Please contact me if you have a MOGG that generates matching keys when using the red keys.

`DeriveKeys` will set the following public variables relevant to MOGG analysis (with versions listed for each one, if a version doesn't match then that variable will contain 0):

---
`10+` `uint Version` - version of MOGG encryption the file uses

`10+` `uint OggOffset` - offset of the OggS data, whether encrypted or not

`10+` `uint HeaderBlockSize` - size of the header in 8-byte blocks

`11+` `uint NonceOffset` - offset of the nonce

`12+` `uint MagicAOffset` - offset of the MagicA hash value

`12+` `uint MagicBOffset` - offset of the MagicB hash value

`12+` `uint Ps3MaskOffset` - offset of the PS3 keymask

`12+` `uint XboxMaskOffset` - offset of the 360 keymask

`12+` `byte[] HvKey` - the 16-byte HVKey used to decrypt the 360 keymask

`12+` `uint KeyIndexOffset` - offset of the stored keyindex, mod 6 for PS3, (mod 6)+6 for 360

`17` `uint V17KeysetOffset` - offset of the value that directs which keyset a v17 MOGG uses

`17` `ulong V17Keyset` - the value that directs which keyset a v17 MOGG uses

`12+` `byte[] MagicA` - the MagicA hash value

`12+` `byte[] MagicB` - the MagicB hash value

`12+` `uint Ps3Index` - KeyIndex mod 6, used for PS3

`12+` `uint XboxIndex` - (KeyIndex mod 6) + 6, used for 360 and others

`11+` `byte[] XboxAesKey` - final derived 16-byte 360 key, equal to the static key for v11

`11+` `byte[] Ps3AesKey` - final derived 16-byte PS3 key, equal to the static key for v11

`11+` `byte[] Nonce` - 16-byte nonce/IV used to initialize the decryptor for the first block

`12+` `byte[] Ps3Mask` - 16-byte keymask used as the final step in deriving the PS3 key

`12+` `byte[] XboxMask` - 16-byte keymask, decrypted by HvKey and then used as the final step in deriving the 360 key

`12+` `byte[] XboxMaskDec` - 16-byte decrypted 360 keymask

`12+` `byte[] Ps3GrindArrayResult` - result of the final step *before* the keymask, xor with XboxAesKey to get Ps3FixedMask

`12+` `byte[] Ps3FixedMask` - the correct PS3 mask in case of mismatch, treating the 360 key as authoritative

`12+` `bool KeymaskMismatch` - this bool indicates a mismatch in derived keys when true

`11+` `bool isC3Mogg` - this bool indicates if a known C3 mogg was detected

---

This library will **not** patch any MOGGs. You are responsible for doing that using the `Ps3MaskOffset`, `Ps3FixedMask`, and `KeymaskMismatch` values. If `KeymaskMismatch` is true after running DeriveKeys, then open the MOGG file, seek to the value stored in `Ps3MaskOffset`, overwrite the next 16 bytes with the contents of `Ps3FixedMask`, then close the file. If you patched it correctly, re-running the process on the patched file will report matching keys.

# Recommended key match algorithm

Read the MOGG header.

Feed the header to `DeriveKeys` with `red` set to **false**.

If `Version` is 11 and `isC3Mogg` is true, then the MOGG is an "OG" version of C3 MOGG that used RB1 encryption.

If `Version` is 12 and `isC3Mogg` is true, then the MOGG is the older style of C3 encryption from before v13 began being used.

If `Version` is 13 and `isC3Mogg` is true, then the MOGG is the newer style of C3 encryption that is marked v13, and probably the most common.

If `KeymaskMismatch` is true, then call `DeriveKeys` again with the same header, but with the bool set to **true**.

If `KeymaskMismatch` is still true after the second call to `DeriveKeys`, then it's not a red key MOGG and you need to call `DeriveKeys` with the bool set to **false** to ensure the 360-related variables are set up for the green keys, and you can optionally patch the MOGG with the keymask stored in `Ps3FixedMask`.

If `KeymaskMismatch` is *false* after the second `DeriveKeys` call, then you potentially have a red key MOGG. Please contact the author of this tool if you encounter this scenario.
