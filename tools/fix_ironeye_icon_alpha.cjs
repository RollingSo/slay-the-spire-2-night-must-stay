const sharp = require(
  "C:/Users/wenti/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp"
);

const sources = [
  "ironeye_assets/character_icon_ironeye.png",
  "ironeye_assets/character_icon_ironeye_outline.png",
];

async function removeNavyBackdrop(path) {
  const source = await sharp(path)
    .ensureAlpha()
    .raw()
    .toBuffer({ resolveWithObject: true });
  const pixels = source.data;

  for (let i = 0; i < pixels.length; i += 4) {
    const red = pixels[i];
    const green = pixels[i + 1];
    const blue = pixels[i + 2];

    // The source portrait's backdrop is consistently blue-black, while the
    // hood, feathers, face and inked silhouette are neutral/olive.  Key only
    // on blue dominance so the character's deliberately black face survives.
    const blueVsRed = (blue - red) / 15;
    const blueVsGreen = (blue - green) / 10;
    const backdropScore = Math.min(blueVsRed, blueVsGreen);
    const removal = Math.max(0, Math.min(1, (backdropScore - 0.12) / 0.38));
    pixels[i + 3] = Math.round(255 * (1 - removal));
  }

  const transparentPortrait = await sharp(pixels, {
    raw: source.info,
  })
    .resize(64, 64, { fit: "contain", kernel: sharp.kernel.lanczos3 })
    .png()
    .toBuffer();

  await sharp(transparentPortrait).toFile(path);
}

(async () => {
  for (const source of sources)
    await removeNavyBackdrop(source);
})().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
