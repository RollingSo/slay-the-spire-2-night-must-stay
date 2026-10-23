"""抠图：按背景类型选不同策略。

- 深色/彩色背景：逐行估背景 + grabCut + 边缘去污染（原 build_seb.matte）
- 白/浅色背景：用深色描边当围栏，从画面边缘灌水，灌不进去的就是角色

白底浅色角色如果用颜色距离法，会把「接近白」的内部误判成背景边缘，
表现为大片半透明、整体像玻璃或碎掉。描边灌水法没有这个问题。
"""
from __future__ import annotations

import cv2
import numpy as np


def _row_background(rgb: np.ndarray, margin: int = 24) -> np.ndarray:
    left = np.median(rgb[:, :margin].astype(np.float32), axis=1)
    right = np.median(rgb[:, -margin:].astype(np.float32), axis=1)
    return ((left + right) / 2)[:, None]


def matte_dark(rgb: np.ndarray, wm: np.ndarray | None = None,
               fg_threshold: float = 55.0, bg_threshold: float = 9.0) -> np.ndarray:
    """深色或彩色背景。rgb: HxWx3 uint8；wm: 水印掩膜（True=抹掉）。"""
    h, w = rgb.shape[:2]
    bg = _row_background(rgb)
    diff = np.sqrt(np.mean((rgb.astype(np.float32) - bg) ** 2, axis=2))
    mask = np.where(diff > 16, cv2.GC_PR_FGD, cv2.GC_PR_BGD).astype('uint8')
    mask[diff > fg_threshold] = cv2.GC_FGD
    mask[diff < bg_threshold] = cv2.GC_BGD
    mask[:8] = cv2.GC_BGD
    mask[:, :16] = cv2.GC_BGD
    mask[:, -12:] = cv2.GC_BGD
    mask[-6:] = cv2.GC_BGD
    if wm is not None:
        mask[wm] = cv2.GC_BGD
    cv2.grabCut(cv2.cvtColor(rgb, cv2.COLOR_RGB2BGR), mask, None,
                np.zeros((1, 65), np.float64), np.zeros((1, 65), np.float64), 2,
                cv2.GC_INIT_WITH_MASK)
    binary = np.isin(mask, [cv2.GC_FGD, cv2.GC_PR_FGD]).astype('uint8')
    n, labels, stats, _ = cv2.connectedComponentsWithStats(binary, 8)
    keep = np.zeros(n, bool)
    keep[1:] = stats[1:, cv2.CC_STAT_AREA] >= 60
    binary = keep[labels].astype('uint8')
    interior = cv2.erode(binary, np.ones((3, 3), np.uint8))
    exterior = cv2.dilate(binary, np.ones((3, 3), np.uint8))
    edge = (exterior > 0) & (interior == 0)
    seed = (interior > 0) & (diff > 20)
    best = np.zeros_like(rgb, dtype=np.float32)
    best_dist = np.full((h, w), np.inf)
    for dy in range(-2, 3):
        for dx in range(-2, 3):
            valid = np.roll(seed, (dy, dx), (0, 1))
            cost = dx * dx + dy * dy
            take = valid & (cost < best_dist)
            best[take] = np.roll(rgb, (dy, dx), (0, 1))[take]
            best_dist[take] = cost
    b = bg
    vec = best - b
    a = np.clip(np.sum((rgb - b) * vec, axis=2) / np.maximum(np.sum(vec * vec, axis=2), 1), 0, 1)
    alpha = binary.astype(np.float32)
    recover = edge & np.isfinite(best_dist) & (diff > 4)
    alpha[recover] = a[recover]
    alpha[diff < 4] = 0
    alpha[interior > 0] = 1
    if wm is not None:
        alpha[wm] = 0
    cleaned = rgb.astype(np.float32)
    solid = (alpha > .04) & (alpha < .995)
    cleaned[solid] = np.clip((rgb[solid] - (1 - alpha[solid, None]) * np.repeat(b, w, axis=1)[solid])
                             / alpha[solid, None], 0, 255)
    alpha[alpha < .04] = 0
    cleaned[alpha == 0] = 0
    return np.dstack([np.rint(cleaned).astype('uint8'), np.rint(alpha * 255).astype('uint8')])


def _corner_background(rgb: np.ndarray, patch: int = 60) -> np.ndarray:
    corners = np.concatenate([rgb[:patch, :patch].reshape(-1, 3), rgb[:patch, -patch:].reshape(-1, 3),
                              rgb[-patch:, :patch].reshape(-1, 3), rgb[-patch:, -patch:].reshape(-1, 3)])
    return np.median(corners.astype(np.float32), axis=0)


def matte_light(rgb: np.ndarray, wm: np.ndarray | None = None,
                threshold: float = 8.0) -> np.ndarray:
    """白/浅色背景：用「与背景色差」当围栏，从画面边缘灌水。

    浅色角色（白裙、白灵体）不能用亮度当围栏——裙面本身接近白，会被当成背景；
    也不能用逐行估背景——角色铺满画面时，左右边缘取到的其实是角色像素。
    """
    h, w = rgb.shape[:2]
    bg = _corner_background(rgb)
    diff = np.sqrt(((rgb.astype(np.float32) - bg) ** 2).mean(axis=2))
    barrier = diff > threshold
    if wm is not None:
        barrier = barrier | wm
    barrier = cv2.dilate(barrier.astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool)
    free = (~barrier).astype(np.uint8)
    mask = np.zeros((h + 2, w + 2), np.uint8)
    cv2.floodFill(free, mask, (0, 0), 2)
    alpha = (free != 2).astype(np.float32)
    alpha = cv2.GaussianBlur(alpha, (3, 3), 0.6)
    alpha[alpha < 0.2] = 0
    rgb_out = rgb.copy()
    rgb_out[alpha == 0] = 0
    return np.dstack([rgb_out, np.rint(alpha * 255).astype('uint8')])


def matte(rgb: np.ndarray, wm: np.ndarray | None = None) -> np.ndarray:
    """按背景亮度自动选策略。"""
    corner = np.concatenate([rgb[:48, :48].reshape(-1, 3), rgb[-48:, -48:].reshape(-1, 3)])
    if corner.mean() > 170:
        return matte_light(rgb, wm)
    return matte_dark(rgb, wm)
