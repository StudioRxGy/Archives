import tkinter as tk
import threading
import time
import random

tips = [
    "要记得喝水哦 ☕",
    "要好好吃饭 🍚",
    "天冷了多穿衣服 🧥",
    "今天也要开心呀 🌸",
    "早点睡觉哦 🌙",
    "要好好爱自己 ❤️",
]

colors = [
    "#FF9AA2", "#FFB7B2", "#FFDAC1",
    "#E2F0CB", "#B5EAD7", "#C7CEEA"
]

WIDTH, HEIGHT = 320, 120


def fade_in(win, step=0.05):
    alpha = 0.0
    while alpha < 1.0:
        alpha += step
        win.attributes("-alpha", alpha)
        time.sleep(0.02)


def create_round_rect(canvas, x1, y1, x2, y2, r, **kwargs):
    canvas.create_arc(x1, y1, x1+r*2, y1+r*2, start=90, extent=90, style="pieslice", **kwargs)
    canvas.create_arc(x2-r*2, y1, x2, y1+r*2, start=0, extent=90, style="pieslice", **kwargs)
    canvas.create_arc(x1, y2-r*2, x1+r*2, y2, start=180, extent=90, style="pieslice", **kwargs)
    canvas.create_arc(x2-r*2, y2-r*2, x2, y2, start=270, extent=90, style="pieslice", **kwargs)
    canvas.create_rectangle(x1+r, y1, x2-r, y2, **kwargs)
    canvas.create_rectangle(x1, y1+r, x2, y2-r, **kwargs)


def create_tip():
    win = tk.Toplevel()
    win.overrideredirect(True)
    win.attributes("-alpha", 0.0)

    bg = random.choice(colors)

    x = random.randint(100, 1300)
    y = random.randint(100, 700)
    win.geometry(f"{WIDTH}x{HEIGHT}+{x}+{y}")

    canvas = tk.Canvas(win, width=WIDTH, height=HEIGHT, highlightthickness=0)
    canvas.pack()

    # 阴影
    create_round_rect(canvas, 6, 6, WIDTH, HEIGHT, 20, fill="#000000", outline="")

    # 主背景
    create_round_rect(canvas, 0, 0, WIDTH-6, HEIGHT-6, 20, fill=bg, outline="")

    canvas.create_text(
        WIDTH//2 - 3,
        HEIGHT//2 - 3,
        text=random.choice(tips),
        fill="white",
        font=("微软雅黑", 14, "bold"),
        width=260,
        justify="center"
    )

    threading.Thread(target=fade_in, args=(win,), daemon=True).start()


def start():
    while True:
        create_tip()
        time.sleep(random.uniform(1.2, 2.5))


if __name__ == "__main__":
    root = tk.Tk()
    root.withdraw()
    threading.Thread(target=start, daemon=True).start()
    root.mainloop()
