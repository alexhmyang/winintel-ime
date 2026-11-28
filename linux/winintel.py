#!/usr/bin/env python3
# 数萌AI服务器 (winintel) / Alex YANG · GPL-3.0-or-later
"""Native IBus engine with a 25-candidate GTK popup (IBus tables cap at 16)."""
import ctypes
import os
import sys
from pathlib import Path
import gi
gi.require_version("IBus", "1.0")
gi.require_version("Gtk", "3.0")
from gi.repository import IBus, Gtk, Gdk, GLib, Pango

ROOT = Path(__file__).resolve().parent
LIB = ctypes.CDLL(str(ROOT / "libwinintel-engine.so"))
for name, args, result in [
    ("open", [ctypes.c_char_p], ctypes.c_void_p),
    ("close", [ctypes.c_void_p], None),
    ("query", [ctypes.c_void_p, ctypes.c_char_p, ctypes.c_int], ctypes.c_int),
    ("candidate", [ctypes.c_void_p, ctypes.c_int], ctypes.c_char_p),
    ("learn", [ctypes.c_void_p, ctypes.c_char_p, ctypes.c_int], None),
    ("selected", [ctypes.c_void_p], ctypes.c_int),
    ("pending", [ctypes.c_void_p], ctypes.c_int),
    ("digit", [ctypes.c_void_p, ctypes.c_int], ctypes.c_int),
    ("confirm", [ctypes.c_void_p], ctypes.c_int),
    ("page", [ctypes.c_void_p, ctypes.c_int], None),
    ("move", [ctypes.c_void_p, ctypes.c_int, ctypes.c_int], None),
    ("cell", [ctypes.c_void_p, ctypes.c_int] + [ctypes.c_double] * 4, None),
]:
    fn = getattr(LIB, "winintel_" + name)
    fn.argtypes, fn.restype = args, result

class Engine(IBus.Engine):
    def __init__(self, **kwargs):
        super().__init__(**kwargs)
        self.handle = LIB.winintel_open(os.fsencode(ROOT / "dictionary.bin"))
        if not self.handle:
            raise RuntimeError("Cannot open offline dictionary")
        self.code, self.english, self.count, self.secure = "", False, 0, False
        self.shift_down, self.shift_used = False, False
        self.window = Gtk.Window(type=Gtk.WindowType.POPUP)
        self.window.set_accept_focus(False)
        self.window.set_type_hint(Gdk.WindowTypeHint.TOOLTIP)
        self.window.set_default_size(620, 120)
        self.box = Gtk.Box(orientation=Gtk.Orientation.VERTICAL, spacing=4)
        self.window.add(self.box)
        self.header = Gtk.Label(xalign=0)
        self.box.pack_start(self.header, False, False, 0)
        self.flow = Gtk.Fixed()
        scroll = Gtk.ScrolledWindow()
        scroll.set_policy(Gtk.PolicyType.NEVER, Gtk.PolicyType.AUTOMATIC)
        scroll.set_max_content_height(280)
        scroll.set_propagate_natural_height(True)
        scroll.add(self.flow)
        self.box.pack_start(scroll, True, True, 0)
        navigation = Gtk.Box(spacing=6)
        for label, direction in [("上一页", -1), ("下一页", 1)]:
            button = Gtk.Button(label=label)
            button.set_can_focus(False)
            button.connect("clicked", lambda _, d=direction: self.page(d))
            navigation.pack_start(button, False, False, 0)
        self.box.pack_start(navigation, False, False, 0)
        self.flow.connect("size-allocate", self.geometry)
        css = Gtk.CssProvider()
        css.load_from_data(b".chosen { background: #168a51; color: white; }")
        Gtk.StyleContext.add_provider_for_screen(Gdk.Screen.get_default(), css, 600)
        self.cursor = (0, 0, 1, 20)

    def geometry(self, *_):
        for local, child in enumerate(self.flow.get_children()):
            rect = child.get_allocation()
            LIB.winintel_cell(self.handle, local, rect.x, rect.y, rect.width, rect.height)

    def refresh(self, query=True):
        if not self.code:
            self.window.hide()
            self.update_preedit_text(IBus.Text.new_from_string(""), 0, False)
            return
        if query:
            self.count = LIB.winintel_query(self.handle, self.code.encode(), int(self.english))
        selected = LIB.winintel_selected(self.handle)
        start = selected // 25 * 25
        pending = LIB.winintel_pending(self.handle)
        self.header.set_text(f"{self.code}  ·  第 {start//25+1} 页 / {(self.count+24)//25} 页"
                             + (f"  ·  编号 {pending}…" if pending else ""))
        self.update_preedit_text(IBus.Text.new_from_string(self.code), len(self.code), True)
        for child in self.flow.get_children():
            self.flow.remove(child)
        x, y, row_height = 0, 0, 40
        for index in range(start, min(start + 25, self.count)):
            text = LIB.winintel_candidate(self.handle, index).decode()
            button = Gtk.Button()
            label = Gtk.Label(label=f"{index-start+1} {text}")
            label.set_line_wrap(True)
            label.set_line_wrap_mode(Pango.WrapMode.WORD_CHAR)
            label.set_max_width_chars(28)
            layout = label.create_pango_layout(label.get_text())
            natural_width, _ = layout.get_pixel_size()
            width = min(604, max(64, natural_width + 28))
            layout.set_width((width - 24) * Pango.SCALE)
            layout.set_wrap(Pango.WrapMode.WORD_CHAR)
            _, text_height = layout.get_pixel_size()
            height = max(40, text_height + 16)
            if x + width > 604:
                x, y, row_height = 0, y + row_height + 4, 40
            button.add(label)
            button.set_can_focus(False)
            button.set_size_request(width, height)
            if index == selected:
                button.get_style_context().add_class("chosen")
            button.connect("clicked", lambda _, i=index: self.choose(i))
            self.flow.put(button, x, y)
            x += width + 4
            row_height = max(row_height, height)
        self.flow.set_size_request(604, y + row_height)
        self.window.show_all()
        self.position()

    def position(self):
        x, y, w, h = self.cursor
        screen = self.window.get_screen()
        monitor = screen.get_monitor_workarea(screen.get_monitor_at_point(x, y))
        width, height = self.window.get_size()
        x = max(monitor.x, min(x, monitor.x + monitor.width - width))
        y = y + h if y + h + height <= monitor.y + monitor.height else y - height
        self.window.move(x, max(monitor.y, y))

    def choose(self, index):
        text = LIB.winintel_candidate(self.handle, index)
        if text:
            if not self.english:
                LIB.winintel_learn(self.handle, self.code.encode(), index)
            self.commit_text(IBus.Text.new_from_string(text.decode()))
            self.code = ""
            self.refresh(False)

    def page(self, direction):
        LIB.winintel_page(self.handle, direction)
        self.refresh(False)

    def do_process_key_event(self, keyval, keycode, state):
        if self.secure:
            return False
        if keyval in (IBus.KEY_Shift_L, IBus.KEY_Shift_R):
            if not state & IBus.ModifierType.RELEASE_MASK:
                self.shift_down, self.shift_used = True, False
                return False
            toggle = self.shift_down and not self.shift_used
            self.shift_down = False
            if toggle:
                if self.code:
                    self.commit_text(IBus.Text.new_from_string(self.code))
                self.code = ""
                self.english = not self.english
                self.refresh(False)
            return toggle
        if state & IBus.ModifierType.RELEASE_MASK:
            return False
        if self.shift_down:
            self.shift_used = True
        if state & (IBus.ModifierType.CONTROL_MASK | IBus.ModifierType.MOD1_MASK | IBus.ModifierType.SUPER_MASK):
            return False
        if keyval == IBus.KEY_Escape and self.code:
            self.code = ""
            self.refresh(False)
            return True
        if self.code:
            if keyval == IBus.KEY_BackSpace:
                self.code = self.code[:-1]
                self.refresh()
                return True
            if IBus.KEY_0 <= keyval <= IBus.KEY_9:
                index = LIB.winintel_digit(self.handle, keyval - IBus.KEY_0)
                if index >= 0:
                    self.choose(index)
                else:
                    self.refresh(False)
                return True
            if keyval in (IBus.KEY_space, IBus.KEY_Return, IBus.KEY_Tab):
                self.choose(LIB.winintel_confirm(self.handle))
                return True
            if keyval in (IBus.KEY_Page_Up, IBus.KEY_Page_Down):
                self.page(-1 if keyval == IBus.KEY_Page_Up else 1)
                return True
            directions = {IBus.KEY_Left: (-1, 0), IBus.KEY_Right: (1, 0),
                          IBus.KEY_Up: (0, -1), IBus.KEY_Down: (0, 1)}
            if keyval in directions:
                self.geometry()
                LIB.winintel_move(self.handle, *directions[keyval])
                self.refresh(False)
                return True
        ch = IBus.keyval_to_unicode(keyval)
        if isinstance(ch, int):
            ch = chr(ch)
        if ch.isascii() and ch.isalpha() and len(self.code) < 256:
            self.code += ch if self.english else ch.lower()
            self.refresh()
            return True
        if self.code and ch.isprintable():
            self.choose(LIB.winintel_confirm(self.handle))
        return False

    def do_set_cursor_location(self, x, y, w, h):
        self.cursor = (x, y, w, h)
        self.position()

    def do_set_content_type(self, purpose, hints):
        self.secure = purpose in (IBus.InputPurpose.PASSWORD, IBus.InputPurpose.PIN)
        if self.secure:
            self.do_reset()

    def do_reset(self):
        self.code = ""
        self.refresh(False)

    def do_focus_out(self):
        self.do_reset()

    def do_disable(self):
        self.do_reset()

    def do_destroy(self):
        self.window.destroy()
        LIB.winintel_close(self.handle)
        self.handle = None
        super().do_destroy()

def main():
    IBus.init()
    bus = IBus.Bus()
    if not bus.is_connected():
        raise RuntimeError("Start ibus-daemon before enabling winintel")
    factory = IBus.Factory.new(bus.get_connection())
    factory.add_engine("winintel", Engine.__gtype__)
    bus.request_name("org.freedesktop.IBus.Winintel", 0)
    bus.connect("disconnected", lambda *_: Gtk.main_quit())
    Gtk.main()

if __name__ == "__main__":
    main()
