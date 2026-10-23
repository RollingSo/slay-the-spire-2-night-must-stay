"""图像工具。"""
import numpy as np
from PIL import Image


def premultiplied_resize(im, scale):
    """预乘 alpha 缩放。

    透明区的 RGB 是 0，直接缩放会让边缘像素的预乘值趋近 0；还原颜色时
    除以一个极小的 alpha，噪声被放大成白点，整圈轮廓发白（看起来像半透明）。
    先把不透明区的颜色向外扩几像素再缩放，边缘就能拿到正确颜色。
    """
    import cv2
    src = np.asarray(im, dtype=np.float32) / 255.0
    alpha = src[..., 3:4]
    rgb = src[..., :3]
    solid = alpha[..., 0] > 0.5
    ext = rgb.copy()
    have = solid.copy()
    kernel = np.ones((3, 3), np.float32)
    for _ in range(6):
        num = cv2.filter2D(ext * have[..., None], -1, kernel)
        den = cv2.filter2D(have.astype(np.float32), -1, kernel)
        cand = np.divide(num, den[..., None], out=np.zeros_like(ext), where=den[..., None] > 0)
        grow = (~have) & (den > 0)
        ext[grow] = cand[grow]
        have = have | grow
    pre = np.concatenate([np.clip(ext, 0, 1) * alpha, alpha], axis=2)
    small = Image.fromarray((np.clip(pre, 0, 1) * 255).round().astype(np.uint8), "RGBA").resize(
        (max(1, round(im.width * scale)), max(1, round(im.height * scale))), Image.LANCZOS)
    back = np.asarray(small, dtype=np.float32) / 255.0
    a2 = np.clip(back[..., 3:4], 0, 1)
    rgb2 = np.divide(back[..., :3], np.maximum(a2, 1e-4), out=np.zeros_like(back[..., :3]),
                     where=a2 > 0)
    rgb2 = np.clip(rgb2, 0, 1)
    solid = a2[..., 0] > 0.9
    have = solid.copy()
    for _ in range(3):
        num = cv2.filter2D(rgb2 * have[..., None], -1, kernel)
        den = cv2.filter2D(have.astype(np.float32), -1, kernel)
        avg = np.divide(num, den[..., None], out=np.zeros_like(rgb2), where=den[..., None] > 0)
        take = (~have) & (den > 0)
        rgb2[take] = avg[take]
        have = have | take
    out = np.concatenate([rgb2, a2], axis=2)
    rgba = (out * 255).round().astype(np.uint8)
    rgba[rgba[..., 3] < 16] = 0
    return Image.fromarray(rgba, "RGBA")
