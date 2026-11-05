"""Enemies (검은 탑의 마수) and awakened hunters, drawn facing right (flip in Unity).

Enemies: ash-grey bodies with magenta/crimson cracks and eyes; organic, spiky shapes.
Hunters: clean shapes, light faces, coloured coats; ally cyan accents only.
"""
import math

from artlib import Sprite, hexc, shade, mix, alpha

ASH = hexc('77737C')
ASH_DARK = hexc('4B4752')
CRACK = hexc('E0406A')
CRACK_HOT = hexc('FF8FB0')
OBSIDIAN = hexc('2A2433')


def leg(s, x, y, length, width, color):
    s.poly([(x - width / 2, y), (x + width / 2, y), (x + width * 0.4, y + length), (x - width * 0.6, y + length)], color)
    s.ellipse(x - width * 0.1, y + length, width * 0.7, width * 0.38, shade(color, 0.8))


def eye(s, x, y, r):
    s.ellipse(x, y, r * 1.3, r, CRACK)
    s.ellipse(x + r * 0.2, y - r * 0.1, r * 0.5, r * 0.4, CRACK_HOT)


def hound(lean=False, tint=None):
    """잿빛 들개 (grunt) / 질주 들개 (runner)."""
    s = Sprite(200, 150)
    body = ASH if tint is None else mix(ASH, tint, 0.35)
    dark = shade(body, 0.62)
    by = 82
    leg_len = 34 if lean else 28
    # Far legs (darker), body, near legs.
    leg(s, 60, by + 8, leg_len, 13, shade(dark, 0.85))
    leg(s, 128, by + 6, leg_len, 13, shade(dark, 0.85))
    # Tail of ash spikes.
    s.poly([(52, by - 6), (14, by - 30 if not lean else by - 18), (30, by - 8), (8, by - 10), (40, by + 6)], dark)
    rx = 52 if not lean else 48
    ry = 25 if not lean else 19
    s.ellipse_grad(92, by - 4, rx, ry, shade(body, 1.18), shade(body, 0.72))
    # Back spines (black tower shards).
    for i, (x, h) in enumerate([(66, 20), (82, 28), (98, 24), (112, 16)]):
        s.poly([(x - 7, by - ry + 4), (x + 1, by - ry - h), (x + 8, by - ry + 6)], OBSIDIAN)
        s.poly([(x - 2, by - ry + 2), (x + 1, by - ry - h), (x + 2, by - ry + 2)], hexc('4C4258'))
    # Crack lines along the flank.
    s.line([(70, by - 2), (86, by + 6), (100, by - 1), (114, by + 6)], 2.6, CRACK)
    leg(s, 72, by + 12, leg_len, 15, body)
    leg(s, 120, by + 10, leg_len, 15, body)
    # Head: wedge snout, ears.
    hx, hy = 146, by - 16
    s.poly([(hx - 12, hy - 18), (hx + 12, hy - 34 if not lean else hy - 30), (hx + 8, hy - 10)], dark)
    s.ellipse_grad(hx, hy, 24, 20, shade(body, 1.2), shade(body, 0.75))
    s.poly([(hx + 8, hy - 10), (hx + 46, hy + 2), (hx + 40, hy + 12), (hx + 6, hy + 14)], shade(body, 0.95))
    s.poly([(hx + 14, hy + 9), (hx + 40, hy + 12), (hx + 22, hy + 16)], hexc('F2E6E8'))
    eye(s, hx + 10, hy - 3, 4.2)
    s.poly([(hx - 4, hy - 16), (hx + 2, hy - 42 if not lean else hy - 38), (hx + 12, hy - 14)], body)
    s.poly([(hx, hy - 18), (hx + 2, hy - 34), (hx + 7, hy - 17)], CRACK)
    return s.finish(outline=2.6, shadow=(6, 6), shadow_alpha=80)


def brute():
    s = Sprite(220, 200)
    body = hexc('6D6A73')
    dark = shade(body, 0.6)
    leg(s, 82, 140, 34, 26, shade(dark, 0.9))
    leg(s, 134, 140, 34, 26, dark)
    s.ellipse_grad(108, 106, 62, 50, shade(body, 1.2), shade(body, 0.66))
    # Belly plate with the magenta core.
    s.ellipse(118, 118, 30, 24, shade(body, 0.85))
    s.ellipse(120, 116, 13, 12, CRACK)
    s.ellipse(123, 113, 6, 5, CRACK_HOT)
    s.glow(120, 116, 22, CRACK, 0.35)
    # Shoulder crystals.
    for x, h, w in ((66, 58, 16), (84, 74, 18), (104, 54, 14), (148, 46, 14)):
        s.poly([(x - w, 78), (x, 78 - h), (x + w, 78)], OBSIDIAN)
        s.poly([(x - w * 0.3, 76), (x, 78 - h), (x + w * 0.1, 76)], hexc('51465E'))
    # Huge forward arm.
    s.ellipse_grad(164, 110, 26, 34, shade(body, 1.1), shade(body, 0.6))
    s.ellipse(176, 144, 22, 15, shade(dark, 0.95))
    for k in range(3):
        s.poly([(166 + k * 9, 150), (172 + k * 9, 166), (178 + k * 9, 150)], hexc('E8DEE2'))
    # Small head sunk between shoulders.
    s.ellipse_grad(138, 74, 22, 19, shade(body, 1.15), shade(body, 0.7))
    eye(s, 146, 72, 4.4)
    s.line([(90, 100), (100, 116), (94, 132)], 3, CRACK)
    return s.finish(outline=3, shadow=(8, 7), shadow_alpha=85)


def siege():
    s = Sprite(264, 180)
    shell = hexc('4E4A56')
    cx, cy = 116, 108
    for k in range(4):
        x = 64 + k * 34
        leg(s, x, cy + 18, 22, 9, shade(shell, 0.65))
    s.ellipse_grad(cx, cy, 86, 42, shade(shell, 1.25), shade(shell, 0.6))
    # Segmented armour plates.
    for k in range(4):
        x = cx - 60 + k * 36
        s.arc(x + 18, cy - 4, 22, 34, 200, 340, 4, shade(shell, 0.55))
    # Heavy siege crystal carried on the back - the reason it ignores blocking.
    s.poly([(cx - 30, cy - 26), (cx - 6, cy - 92), (cx + 14, cy - 30)], hexc('3A2A44'))
    s.poly([(cx - 6, cy - 92), (cx + 14, cy - 30), (cx + 34, cy - 70), (cx + 30, cy - 24)], OBSIDIAN)
    s.line([(cx - 4, cy - 82), (cx + 2, cy - 54), (cx - 6, cy - 36)], 3, CRACK)
    s.glow(cx, cy - 56, 26, CRACK, 0.3)
    # Ram head.
    s.ellipse_grad(cx + 86, cy + 4, 26, 22, shade(shell, 1.1), shade(shell, 0.6))
    s.poly([(cx + 96, cy - 10), (cx + 132, cy + 2), (cx + 98, cy + 16)], hexc('D9CBD0'))
    eye(s, cx + 90, cy - 4, 4)
    return s.finish(outline=3, shadow=(8, 7), shadow_alpha=85)


def boss():
    """잿빛 문지기 - gatekeeper with tower spires on its shoulders."""
    s = Sprite(360, 360)
    body = hexc('5E5966')
    dark = shade(body, 0.6)
    leg(s, 140, 260, 56, 40, dark)
    leg(s, 214, 260, 56, 40, shade(dark, 0.85))
    s.ellipse_grad(178, 200, 104, 88, shade(body, 1.2), shade(body, 0.6))
    # Shoulder spires = miniature black towers.
    for x, h, w in ((96, 100, 24), (130, 124, 28), (232, 116, 26), (262, 86, 20)):
        s.poly([(x - w, 140), (x - w * 0.4, 140 - h), (x + w * 0.4, 140 - h - 10), (x + w, 140)], OBSIDIAN)
        s.poly([(x - w * 0.4, 140 - h), (x + w * 0.4, 140 - h - 10), (x + w * 0.1, 140 - h + 30)], hexc('5C4F6C'))
        s.line([(x, 140 - h + 40), (x + 4, 140 - h + 80), (x - 2, 138)], 3, CRACK)
    # Chest gate - a glowing vertical rift.
    s.ellipse(182, 214, 42, 54, shade(body, 0.8))
    s.poly([(182, 166), (204, 214), (182, 266), (160, 214)], OBSIDIAN)
    s.poly([(182, 180), (194, 214), (182, 250), (170, 214)], CRACK)
    s.poly([(182, 196), (187, 214), (182, 232), (177, 214)], CRACK_HOT)
    s.glow(182, 214, 50, CRACK, 0.45)
    # Masked head.
    s.ellipse_grad(254, 138, 40, 36, shade(body, 1.2), shade(body, 0.7))
    s.poly([(232, 124), (300, 118), (296, 158), (240, 160)], hexc('D8CED2'))
    eye(s, 266, 134, 6)
    eye(s, 288, 132, 5)
    for x in (236, 260):
        s.poly([(x, 108), (x + 6, 70), (x + 16, 106)], OBSIDIAN)
    # Arms.
    s.ellipse_grad(300, 216, 34, 48, shade(body, 1.1), shade(body, 0.6))
    s.ellipse(312, 262, 30, 20, dark)
    s.ellipse_grad(76, 212, 30, 44, shade(body, 0.95), shade(body, 0.55))
    return s.finish(outline=3.5, shadow=(10, 9), shadow_alpha=90)


SKIN = hexc('EFCFB2')


def hunter(kind):
    """0 강윤 (rifle), 1 세희 (scout, twin blades), 2 백도하 (shield commander)."""
    s = Sprite(170, 190)
    cx = 80
    if kind == 0:
        coat, coat2, hair, trim = hexc('2F3B4E'), hexc('243042'), hexc('22242A'), hexc('3FD6C6')
    elif kind == 1:
        coat, coat2, hair, trim = hexc('5E8C5A'), hexc('476E45'), hexc('9A5A32'), hexc('E8F1E0')
    else:
        coat, coat2, hair, trim = hexc('6B7D96'), hexc('51627A'), hexc('3A3D44'), hexc('F0B340')
    # Legs
    s.poly([(cx - 16, 140), (cx - 4, 140), (cx - 6, 172), (cx - 18, 172)], hexc('2A2E36'))
    s.poly([(cx + 4, 140), (cx + 16, 140), (cx + 18, 172), (cx + 6, 172)], hexc('343943'))
    s.ellipse(cx - 12, 174, 10, 5, hexc('1E2128'))
    s.ellipse(cx + 13, 174, 10, 5, hexc('1E2128'))
    # Coat body (trapezoid, hem flares)
    hem = 34 if kind == 0 else 26
    s.poly_grad([(cx - 24, 92), (cx + 24, 92), (cx + hem, 150), (cx - hem, 150)], shade(coat, 1.15), coat2)
    s.poly([(cx - 3, 94), (cx + 3, 94), (cx + 2, 148), (cx - 2, 148)], shade(coat2, 0.8))
    s.line([(cx - hem + 2, 148), (cx + hem - 2, 148)], 3, trim)
    if kind == 2:
        s.poly([(cx - 22, 96), (cx + 22, 96), (cx + 18, 128), (cx - 18, 128)], hexc('8394AC'))
        s.line([(cx - 20, 112), (cx + 20, 112)], 3, trim)
    # Head
    hy = 66
    s.ellipse_grad(cx, hy, 26, 25, shade(SKIN, 1.05), shade(SKIN, 0.86))
    if kind == 0:
        s.poly([(cx - 28, hy - 4), (cx - 22, hy - 28), (cx + 4, hy - 34), (cx + 28, hy - 20), (cx + 26, hy - 2), (cx + 8, hy - 14), (cx - 12, hy - 8)], hair)
        s.poly([(cx - 28, hy - 4), (cx - 32, hy + 18), (cx - 20, hy + 6)], hair)
    elif kind == 1:
        s.poly([(cx - 27, hy - 2), (cx - 20, hy - 26), (cx + 6, hy - 32), (cx + 26, hy - 18), (cx + 24, hy - 4), (cx, hy - 14), (cx - 14, hy - 6)], hair)
        s.poly([(cx - 24, hy - 14), (cx - 52, hy - 6), (cx - 46, hy + 4), (cx - 22, hy - 2)], hair)
        s.ellipse(cx - 24, hy - 10, 4, 4, hexc('E05050'))
    else:
        s.poly([(cx - 26, hy - 6), (cx - 18, hy - 26), (cx + 10, hy - 30), (cx + 26, hy - 16), (cx + 24, hy - 6), (cx - 8, hy - 16)], hair)
        s.line([(cx - 6, hy + 12), (cx + 14, hy + 4)], 2, hexc('B88D74'))
    s.ellipse(cx + 12, hy + 2, 3, 3.6, hexc('1B2230'))
    s.ellipse(cx + 22, hy + 1, 2.6, 3.2, hexc('1B2230'))
    # Weapon / gear (held forward to the right)
    if kind == 0:
        s.poly([(cx - 6, 110), (cx + 74, 104), (cx + 76, 112), (cx - 4, 120)], hexc('B9C4CF'))
        s.poly([(cx + 30, 112), (cx + 40, 112), (cx + 36, 128), (cx + 28, 128)], hexc('4A5260'))
        s.rect(cx + 74, 103, cx + 84, 111, trim)
        s.ellipse(cx + 8, 112, 9, 8, shade(coat, 1.1))
    elif kind == 1:
        for dy, ang in ((106, -18), (122, 10)):
            x0, y0 = cx + 12, dy
            x1 = x0 + 48 * math.cos(math.radians(ang))
            y1 = y0 + 48 * math.sin(math.radians(ang))
            s.line([(x0, y0), (x1, y1)], 5, hexc('DDE7EC'))
            s.line([(x0 - 6, y0), (x0 + 4, y0)], 6, hexc('3A3D44'))
        s.ellipse(cx + 10, 112, 8, 8, shade(coat, 1.1))
    else:
        s.poly([(cx + 26, 84), (cx + 58, 90), (cx + 60, 150), (cx + 30, 160)], hexc('7E8C9E'))
        s.poly([(cx + 30, 88), (cx + 56, 93), (cx + 57, 148), (cx + 33, 156)], hexc('9AA9BC'))
        s.line([(cx + 44, 98), (cx + 45, 146)], 4, trim)
        s.line([(cx + 34, 120), (cx + 56, 122)], 4, trim)
    return s.finish(outline=2.6, shadow=(6, 6), shadow_alpha=80)


def all_units():
    return {
        'enemy-grunt': hound(), 'enemy-runner': hound(lean=True, tint=hexc('C2405A')),
        'enemy-brute': brute(), 'enemy-siege': siege(), 'enemy-boss': boss(),
        'hunter-0': hunter(0), 'hunter-1': hunter(1), 'hunter-2': hunter(2),
    }
