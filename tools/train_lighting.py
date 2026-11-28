"""Fit a small exposure regressor from reference images. Prints JavaScript for the browser.
Usage: python train_lighting.py --ffmpeg /path/to/ffmpeg reference1.jpg ...
Each independent reference is augmented with known monotone exposure curves.
"""
import argparse
import json
import subprocess
from statistics import mean

KNOTS = [0, 8, 16, 32, 64, 96, 128, 160, 192, 224, 240, 248, 255]
# Lower endpoint, upper endpoint and exponent of the simulated exposure.
EXPOSURES = [(0, 255, 1)] * 5 + [
    (0, 100, 1.4), (0, 160, 1.6), (1, 205, 2.2), (4, 230, 2.8),
    (0, 255, 2), (0, 255, 2.8), (0, 255, .55), (0, 255, .36),
    (80, 244, .85), (110, 248, .75), (55, 185, 1), (82, 165, 1),
]


def example(histogram, exposure):
    low, high, exponent = exposure
    transformed = [round(low + (high - low) * (i / 255) ** exponent) for i in range(256)]
    total = sum(histogram)
    features = []
    for fraction in (.05, .5, .95):
        count = 0
        for i, freq in enumerate(histogram):
            count += freq
            if count >= total * fraction:
                features.append(transformed[i])
                break
    correction = []
    for k in KNOTS:
        restored = 255 * min(1, max(0, (k - low) / (high - low))) ** (1 / exponent)
        correction.append(max(k - 72, min(k + 72, restored)))
    correction[0], correction[-1] = 0, 255
    return features, correction


def fit(rows, depth=0):
    averages = [mean(col) for col in zip(*(curve for _, curve in rows))]
    error = sum(sum((a - b) ** 2 for a, b in zip(curve, averages)) for _, curve in rows)
    best = None
    if depth < 4:
        for feature in range(3):
            values = sorted({x[feature] for x, _ in rows})
            for left, right in zip(values, values[1:]):
                threshold = (left + right) // 2
                groups = [[r for r in rows if (r[0][feature] <= threshold) == side] for side in (True, False)]
                if min(map(len, groups)) < 5:
                    continue
                loss = sum(sum(sum((v - mean_col) ** 2 for v in col)
                               for col in zip(*(r[1] for r in group)) for mean_col in [mean(col)])
                           for group in groups)
                if loss < error * .95 and (best is None or loss < best[0]):
                    best = loss, feature, threshold, groups
    if best is None:
        return [round(v) for v in averages]
    _, feature, threshold, (left, right) = best
    return {'feature': feature, 'threshold': threshold, 'left': fit(left, depth+1), 'right': fit(right, depth+1)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ffmpeg', required=True)
    parser.add_argument('images', nargs='+')
    args = parser.parse_args()
    rows = []
    for image in args.images:
        rgb = subprocess.check_output([args.ffmpeg, '-loglevel', 'error', '-i', image, '-vf',
                                      'scale=128:96', '-frames:v', '1', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-'])
        hist = [0] * 256
        for r, g, b in zip(rgb[::3], rgb[1::3], rgb[2::3]):
            hist[(54*r + 183*g + 19*b + 128) >> 8] += 1
        rows.extend(example(hist, exposure) for exposure in EXPOSURES)
    print('const exposureKnots = ' + json.dumps(KNOTS) + ';')
    print('const exposureTree = ' + json.dumps(fit(rows), separators=(',', ':')) + ';')


if __name__ == '__main__':
    main()
