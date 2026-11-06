"""Project copy of gatefall-style v1.1.0 token/contrast validation.

Read-only by default. This checks the manifest, not rendered screenshots.
"""
import argparse
import json
import math
import re
from pathlib import Path

ALIAS = re.compile(r'^\{([^{}]+)\}$')
HEX = re.compile(r'^#[0-9A-Fa-f]{6}$')


class StyleError(ValueError):
    pass


def rgb(color):
    if not isinstance(color, str) or not HEX.fullmatch(color):
        raise StyleError(f'Expected sRGB hex color, got {color!r}')
    return tuple(int(color[i:i+2], 16) / 255 for i in (1, 3, 5))


def luminance(color):
    values = [v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4 for v in color]
    return sum(a * b for a, b in zip(values, (0.2126, 0.7152, 0.0722)))


def contrast(foreground, background):
    low, high = sorted((luminance(foreground), luminance(background)))
    return (high + 0.05) / (low + 0.05)


def validate(data):
    tokens = data['tokens']
    cache = {}

    def token(key, stack=()):
        if key not in tokens:
            raise StyleError(f'Unknown token: {key}')
        if key in stack:
            raise StyleError('Alias cycle: ' + ' -> '.join((*stack, key)))
        if key not in cache:
            cache[key] = resolve(tokens[key]['value'], (*stack, key))
        return cache[key]

    def resolve(value, stack=()):
        if isinstance(value, str):
            match = ALIAS.fullmatch(value)
            if match:
                return token(match.group(1), stack)
            if value.startswith('{') or value.endswith('}'):
                raise StyleError(f'Malformed alias: {value}')
        if isinstance(value, dict):
            return {key: resolve(item, stack) for key, item in value.items()}
        if isinstance(value, list):
            return [resolve(item, stack) for item in value]
        return value

    if data['schemaVersion'] != 1:
        raise StyleError('Unsupported schemaVersion')
    for key, definition in tokens.items():
        value = token(key)
        kind = definition['type']
        if definition['origin'] not in ('gatefall-source', 'gatefall-style-design'):
            raise StyleError(f'{key}: unsupported provenance')
        if kind == 'color':
            rgb(value)
        elif kind in ('dimension', 'duration', 'number'):
            if type(value) not in (int, float) or not math.isfinite(value):
                raise StyleError(f'{key}: expected a finite number')
            if value < 0 or (kind == 'dimension' and value == 0):
                raise StyleError(f'{key}: invalid numeric range')
            if kind == 'dimension' and definition.get('unit') != 'hud-unit':
                raise StyleError(f'{key}: dimensions must be HUD logical units')
            if kind == 'duration' and definition.get('unit') != 'ms':
                raise StyleError(f'{key}: durations must use ms')
        elif kind in ('string', 'fontFamily'):
            if not isinstance(value, str) or not value:
                raise StyleError(f'{key}: expected a nonempty string')
        else:
            raise StyleError(f'{key}: unsupported token type {kind}')

    resolve(data['typography'])
    components = resolve(data['components'])
    roles = resolve(data['roles'])
    required_roles = {'single', 'control', 'blast', 'block', 'mark'}
    if not required_roles.issubset({role['id'] for role in roles}):
        raise StyleError('Missing a base tower role')
    for field in ('id', 'runtimeKind', 'color'):
        values = [role[field] for role in roles]
        if len(values) != len(set(values)):
            raise StyleError(f'Role identity collision: {field}')
    for raw, resolved in zip(data['roles'], roles):
        if raw['color'] != '{color.role.' + raw['id'] + '}':
            raise StyleError(f"{raw['id']}: role color must use its own semantic token")
        rgb(resolved['color'])

    minimum = data['review']['minimumTextContrast']
    if not isinstance(minimum, (float, int)) or minimum < 1:
        raise StyleError('Invalid contrast threshold')
    pairs = []

    def pair(label, foreground, top, bottom):
        fg, start, end = rgb(foreground), rgb(top), rgb(bottom)
        ratios = [contrast(fg, tuple(a + (b-a)*i/20 for a,b in zip(start,end))) for i in range(21)]
        worst = min(ratios)
        pairs.append({'element': label, 'minimumRatio': round(worst, 3)})
        if worst + 1e-9 < minimum:
            raise StyleError(f'{label}: contrast {worst:.3f} below {minimum}')

    def surface(label, item):
        if not isinstance(item, dict):
            return
        if 'textStyle' in item and item['textStyle'] not in data['typography']:
            raise StyleError(f'{label}: unknown textStyle')
        if 'foreground' in item and 'backgroundTop' in item:
            bottom = item.get('backgroundBottom', item['backgroundTop'])
            for field in ('foreground', 'secondaryForeground', 'cost'):
                if field in item:
                    pair(label + '.' + field, item[field], item['backgroundTop'], bottom)
        for key, value in item.items():
            if isinstance(value, dict):
                surface(label + '.' + key, value)

    for name, item in components.items():
        surface(name, item)
    tint = token('card.roleTint.opacity')
    if not 0 <= tint <= 1:
        raise StyleError('Card tint opacity outside 0..1')
    face = rgb(token('color.card.face'))
    for role in roles:
        tinted = tuple(a*(1-tint)+b*tint for a,b in zip(face,rgb(role['color'])))
        for text in ('color.text.primary', 'color.text.secondary', 'color.cost'):
            value = contrast(rgb(token(text)), tinted)
            pairs.append({'element': 'card.tint.' + role['id'] + '.' + text, 'minimumRatio': round(value, 3)})
            if value + 1e-9 < minimum:
                raise StyleError(f"Tinted {role['id']} card {text}: contrast {value:.3f} below {minimum}")
    return {
        'status': 'PASS', 'styleId': data['styleId'], 'version': data['version'],
        'tokens': len(tokens), 'components': len(components), 'roles': len(roles),
        'contrastPairs': len(pairs), 'minimumObservedRatio': min(p['minimumRatio'] for p in pairs),
        'contrastResults': pairs,
        'checks': ['alias resolution and cycles', 'typed values and explicit units', 'provenance namespaces', 'unique role identities', 'component and disabled text contrast', 'role-tinted card text contrast'],
        'scope': 'Static manifest only; rendered gradients, fonts, touch and gameplay require separate review.'
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--tokens', type=Path, default=Path(__file__).resolve().parents[1] / 'Docs' / 'Design' / 'city-barrier-ui-20261106' / 'tokens.json')
    parser.add_argument('--json', type=Path, help='Optional explicit path for the detailed result')
    args = parser.parse_args()
    try:
        data = json.loads(args.tokens.read_text(encoding='utf-8-sig'))
        result = validate(data)
    except (StyleError, KeyError, TypeError, ValueError, OSError) as error:
        print(json.dumps({'status': 'FAIL', 'error': str(error)}, ensure_ascii=False))
        return 1
    if args.json:
        args.json.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({k:v for k,v in result.items() if k != 'contrastResults'}, ensure_ascii=False, indent=2))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
