from pathlib import Path
import hashlib
import json
import shutil

import numpy as np
from PIL import Image, ImageDraw, ImageFilter


SOURCE = Path(r'D:/du an/Zero1/Assets/TripoModels/scary_daruma_3d_model_1/scary_daruma_3d_model_1.fbm/scary_daruma_3d_model_1_basecolor.jpg')
OUTPUT = Path(__file__).parent / 'DarumaTextureEdit'


def main():
    OUTPUT.mkdir(exist_ok=True)
    backup = OUTPUT / 'original_basecolor.jpg'
    if not backup.exists():
        shutil.copy2(SOURCE, backup)
    image = Image.open(backup).convert('RGB')
    assert image.size == (4096, 4096), image.size
    rgb = np.asarray(image).astype(np.float32)
    # Manually trace the large clothing island; no UV coordinates are moved.
    outline = [(0,1100),(46,1005),(95,1020),(153,1040),(244,1057),
               (350,1080),(460,1105),(575,1136),(695,1142),(811,1105),
               (923,1064),(1023,1010),(1125,951),(1206,894),(1283,834),
               (1353,779),(1415,772),(1470,800),(1520,842),(1540,910),(1570,947),
               (1599,971),(1599,1031),(1547,1082),(1508,1150),
               (1470,1236),(1458,1327),(1484,1368),(1525,1385),
               (1466,1456),(1395,1513),(1306,1555),(1196,1573),
               (1094,1570),(1012,1570),(959,1552),(917,1515),
               (935,1437),(857,1435),(866,1381),(809,1377),(766,1399),
               (741,1451),(684,1482),(626,1534),(535,1558),(457,1599),
               (248,1599),(216,1534),(195,1456),(157,1390),
               (112,1388),(82,1365),(0,1340)]
    region = Image.new('L', image.size)
    ImageDraw.Draw(region).polygon([(round(x*2.56), round(y*2.56)) for x,y in outline], fill=255)
    red, green, blue = rgb.transpose(2, 0, 1)
    # Keep desaturated highlights, but do not recolor brown surrounding UV fragments.
    color = ((red > green * 1.28) & (red > blue * 1.12)) | ((red > green * 1.02) & (green > blue * 1.18))
    color |= (red > 175) & (green > 140) & (blue > 110)
    cloth = (np.asarray(region) > 0) & color
    cloth_image = Image.fromarray(cloth.astype('uint8') * 255)
    cloth_image = cloth_image.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.MinFilter(9))
    cloth_image = Image.composite(cloth_image, Image.new('L', image.size), region)
    alpha = np.asarray(cloth_image.filter(ImageFilter.GaussianBlur(1.0))).astype(np.float32) / 255
    # Gold paint is removed before adding stripes, so no drop silhouettes remain.
    gold = (green > red * .48) & (blue < green * .78) & (red > 65) & (alpha > .5)
    gold_image = Image.fromarray(gold.astype('uint8') * 255).filter(ImageFilter.MaxFilter(13))
    gold_alpha = np.asarray(gold_image.filter(ImageFilter.GaussianBlur(2))).astype(np.float32)/255
    luminance = red * .18 + (red * .2126 + green * .7152 + blue * .0722) * .24
    valid = (1 - np.asarray(gold_image).astype(np.float32)/255) * (alpha > .5)
    numerator = np.asarray(Image.fromarray(np.rint(luminance * valid).astype('uint8')).filter(ImageFilter.BoxBlur(65))).astype(np.float32)
    denominator = np.asarray(Image.fromarray((valid*255).astype('uint8')).filter(ImageFilter.BoxBlur(65))).astype(np.float32)/255
    filled = numerator / np.maximum(denominator, .001)
    luminance = luminance * (1-gold_alpha) + filled * gold_alpha
    y, x = np.indices(alpha.shape, dtype=np.float32)
    phase = np.mod(x + y*.23, 145)
    stripe = np.clip(np.minimum(phase-8, 39-phase)/3, 0, 1)
    gray = np.clip(luminance * .68 + 2 + stripe * (9 + luminance*.16), 0, 255)
    edited = rgb * (1-alpha[...,None]) + gray[...,None]*alpha[...,None]
    result = Image.fromarray(np.rint(edited).astype('uint8'))
    result.save(OUTPUT / 'basecolor_black_stripes.png')
    result.save(OUTPUT / 'basecolor_black_stripes.jpg', quality=100, subsampling=0)
    cloth_image.save(OUTPUT / 'clothing_mask.png')
    result.resize((1200,1200), Image.Resampling.LANCZOS).save(OUTPUT / 'preview.jpg', quality=94)
    outside = alpha == 0
    assert np.array_equal(np.asarray(result)[outside], rgb.astype('uint8')[outside])
    assert result.size == image.size
    report = {'size': result.size, 'masked_pixels': int(np.count_nonzero(alpha)),
              'outside_mask_unchanged_png': True,
              'original_sha256': hashlib.sha256(backup.read_bytes()).hexdigest()}
    (OUTPUT / 'verification.json').write_text(json.dumps(report, indent=2))
    print(json.dumps(report))


if __name__ == '__main__':
    main()
