"""Edits the frames recorded in Play mode into the README media.

Workflow (Unity editor, Play mode):
  1. BlockMeow > Media > Capture Screenshots   -> Temp/Media/shots/*.png
  2. BlockMeow > Media > Record Highlight      -> Temp/Media/raw/f*.png + markers.txt
  3. python Tools/media/make_media.py          -> Temp/Media/edited/e*.png, docs/media/highlight.gif, docs/images/*.png
  4. BlockMeow > Media > Encode Highlight Video -> docs/media/highlight.mp4

The captions, the finger and the end card are drawn in the game itself while recording; this script
only does the editing: speed ramps, a slow-motion zoom on the cat skill, a cross-fade and the GIF.
Needs Pillow.
"""
import math
import os
import shutil
import sys
from functools import lru_cache

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
MEDIA = os.path.join(ROOT, 'Temp', 'Media')
RAW = os.path.join(MEDIA, 'raw')
SHOTS = os.path.join(MEDIA, 'shots')
EDITED = os.path.join(MEDIA, 'edited')
DOCS = os.path.join(ROOT, 'docs')
W, H = 720, 1280
STRIP = 100                # the caption strip at the bottom (UIRoot.BannerH at 720 px wide) never zooms
BOARD_CENTER = (360, 520)  # where the slow-motion zoom aims (the board, in 720x1280 frames)


def markers():
    m = {}
    with open(os.path.join(MEDIA, 'markers.txt'), encoding='utf-8') as f:
        for line in f:
            if line.strip():
                name, frame = line.split()
                m[name] = int(frame)
    return m


@lru_cache(maxsize=8)
def raw(i):
    return Image.open(os.path.join(RAW, f'f{i:05d}.png')).convert('RGB')


def smooth(k):
    return k * k * (3 - 2 * k)


# a timeline is a list of frame descriptions; render() turns one into an image
#   ('f', t, zoom, center)  raw frame at time t (fractional t blends neighbours: slow motion)
#   ('x', a, b, w)          cross-fade between two descriptions

def seg(t0, t1, speed=1.0, zoom=None):
    """Raw frames t0..t1 played at `speed`; zoom(k) -> scale for k in 0..1 across the segment."""
    n = max(1, int(round((t1 - t0) / speed)))
    out = []
    for j in range(n):
        t = min(t0 + j * speed, t1 - 1)
        k = j / max(1, n - 1)
        out.append(('f', t, zoom(k) if zoom else 1.0, BOARD_CENTER))
    return out


def join(a, b, n):
    """Appends b to a with an n-frame cross-fade."""
    if n <= 0:
        return a + b
    mixed = [('x', a[len(a) - n + i], b[i], (i + 1) / (n + 1)) for i in range(n)]
    return a[:-n] + mixed + b[n:]


def zoomed(im, z, center):
    if z <= 1.0001:
        return im
    h = H - STRIP
    cw, ch = W / z, h / z
    cx = min(max(center[0], cw / 2), W - cw / 2)
    cy = min(max(center[1], ch / 2), h - ch / 2)
    box = (cx - cw / 2, cy - ch / 2, cx + cw / 2, cy + ch / 2)
    out = im.copy()
    out.paste(im.resize((W, h), Image.LANCZOS, box=box), (0, 0))
    return out


def render(d):
    if d[0] == 'x':
        return Image.blend(render(d[1]), render(d[2]), d[3])
    _, t, z, center = d
    i = int(math.floor(t))
    a = t - i
    im = raw(i) if a < 1e-3 else Image.blend(raw(i), raw(i + 1), a)
    return zoomed(im, z, center)


def timeline(m, last):
    home, play, skill, more, cats, catbox = m['home'], m['play'], m['skill'], m['more'], m['cats'], m['catbox']
    challenge, endcard, end = m['challenge'], m['endcard'], min(m['end'], last + 1)
    hit = skill + 13            # the finger lands on the cat ~0.44 s after the mark
    slow_end = hit + 30

    intro = seg(home, play, 1.15, lambda k: 1.06 - 0.06 * smooth(min(1.0, k * 2.5)))
    game = seg(play, hit)
    slowmo = seg(hit, slow_end, 0.5, lambda k: 1.0 + 0.14 * smooth(min(1.0, k * 2.0)))
    settle = seg(slow_end, more, 1.0, lambda k: 1.14 - 0.14 * smooth(k))
    rush = seg(more, cats, 1.6)
    reveal = seg(cats, catbox + 75)
    back = seg(catbox + 75, challenge, 1.5)
    duel = seg(challenge, endcard)
    outro = seg(endcard, end)
    outro += [outro[-1]] * 15  # hold the end card a little longer

    t = intro + game + slowmo + settle
    t = join(t, rush, 0)
    t = join(t, reveal, 8)      # the game -> cats cut is a hard screen change in the recording
    t = t + back + duel
    t = join(t, outro, 6)
    return t


def write_frames(tl):
    if os.path.isdir(EDITED):
        shutil.rmtree(EDITED)
    os.makedirs(EDITED)
    for n, d in enumerate(tl):
        render(d).save(os.path.join(EDITED, f'e{n:05d}.png'), compress_level=1)
        if n % 60 == 0:
            print(f'  frame {n}/{len(tl)}')


def write_gif(count, path, size=(360, 640), step=2):
    """Every `step`-th edited frame (15 fps), shared palette, no dithering: flat paper colors stay clean."""
    idx = list(range(0, count, step))
    sample = [Image.open(os.path.join(EDITED, f'e{i:05d}.png')).convert('RGB').resize(size, Image.LANCZOS)
              for i in idx[:: max(1, len(idx) // 24)]]
    sheet = Image.new('RGB', (size[0] * len(sample), size[1]))
    for k, im in enumerate(sample):
        sheet.paste(im, (k * size[0], 0))
    palette = sheet.quantize(colors=255, method=Image.Quantize.MEDIANCUT)
    frames = []
    for i in idx:
        im = Image.open(os.path.join(EDITED, f'e{i:05d}.png')).convert('RGB').resize(size, Image.LANCZOS)
        frames.append(im.quantize(palette=palette, dither=Image.Dither.NONE))
    durations = [70 if k % 3 != 1 else 60 for k in range(len(frames))]  # ~15 fps
    os.makedirs(os.path.dirname(path), exist_ok=True)
    frames[0].save(path, save_all=True, append_images=frames[1:], duration=durations, loop=0, optimize=True)
    print(f'  gif {path}: {len(frames)} frames, {os.path.getsize(path) / 1e6:.1f} MB')


def write_screenshots():
    out = os.path.join(DOCS, 'images')
    os.makedirs(out, exist_ok=True)
    for name in sorted(os.listdir(SHOTS)):
        if name.endswith('.png'):
            im = Image.open(os.path.join(SHOTS, name)).convert('RGB').resize((540, 960), Image.LANCZOS)
            im.save(os.path.join(out, name), optimize=True)
            print(f'  {name}: {os.path.getsize(os.path.join(out, name)) // 1024} KB')


def main():
    if os.path.isdir(SHOTS):
        print('screenshots')
        write_screenshots()
    if not os.path.isdir(RAW):
        print('no recording in', RAW)
        return 1
    last = len([f for f in os.listdir(RAW) if f.endswith('.png')]) - 1
    tl = timeline(markers(), last)
    print(f'highlight: {last + 1} raw frames -> {len(tl)} edited frames ({len(tl) / 30:.1f} s)')
    write_frames(tl)
    write_gif(len(tl), os.path.join(DOCS, 'media', 'highlight.gif'))
    return 0


if __name__ == '__main__':
    sys.exit(main())
