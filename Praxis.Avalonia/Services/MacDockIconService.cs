using System.Runtime.InteropServices;

namespace Praxis.Avalonia.Services;

internal static class MacDockIconService
{
    private const string ObjCLibrary = "/usr/lib/libobjc.A.dylib";

    public static void ApplyIfNeeded()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "praxis-dock-icon.png");
        if (!File.Exists(iconPath))
        {
            return;
        }

        try
        {
            Apply(iconPath);
        }
        catch
        {
            // Dock icon setup is cosmetic; startup should not fail if AppKit rejects the image.
        }
    }

    private static void Apply(string iconPath)
    {
        var applicationClass = objc_getClass("NSApplication");
        var sharedApplication = objc_msgSend(applicationClass, sel_registerName("sharedApplication"));
        if (sharedApplication == IntPtr.Zero)
        {
            return;
        }

        var imageClass = objc_getClass("NSImage");
        var image = objc_msgSend(
            objc_msgSend(imageClass, sel_registerName("alloc")),
            sel_registerName("initWithContentsOfFile:"),
            ToNSString(iconPath));

        if (image == IntPtr.Zero)
        {
            return;
        }

        objc_msgSend(sharedApplication, sel_registerName("setApplicationIconImage:"), image);
        objc_msgSend(image, sel_registerName("release"));
    }

    private static IntPtr ToNSString(string value)
        => objc_msgSend(
            objc_getClass("NSString"),
            sel_registerName("stringWithUTF8String:"),
            value);

    [DllImport(ObjCLibrary)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjCLibrary)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr argument);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(
        IntPtr receiver,
        IntPtr selector,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string argument);
}
