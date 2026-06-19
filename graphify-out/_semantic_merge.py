import json
from pathlib import Path

base_root = r"D:\Workspace\Vim_with_mouse"

# Load AST extraction
ast = json.loads(Path('graphify-out/.graphify_ast.json').read_text(encoding='utf-8'))
print(f'AST nodes: {len(ast["nodes"])}, edges: {len(ast["edges"])}')

# Manual semantic extraction from docs and images read by the agent
semantic = {'nodes': [], 'edges': [], 'hyperedges': [], 'input_tokens': 0, 'output_tokens': 0}

docs = [
    ('readme_hunt_and_peck', 'Hunt and Peck', 'document'),
    ('readme_ui_automation_navigation', 'UI Automation Navigation', 'concept'),
    ('changelog_v1_1_elevated_modes', 'v1.1 - Elevated & Popup-friendly', 'document'),
    ('changelog_interaction_modes', 'Interaction Modes (Left/Right Click)', 'concept'),
    ('claude_graphify_config', 'Graphify Configuration', 'concept'),
]

images = [
    ('screenshot_explorer_overlay', 'Explorer Screenshot - Hint Overlay', 'image'),
    ('screenshot_visual_studio_overlay', 'Visual Studio Screenshot - Hint Overlay', 'image'),
]

for nid, label, ftype in docs + images:
    semantic['nodes'].append({
        'id': nid, 'label': label, 'file_type': ftype,
        'source_file': base_root + '\\README.md' if 'readme' in nid or 'ui_automation' in nid else
                       base_root + '\\CHANGELOG.md' if 'changelog' in nid else
                       base_root + '\\CLAUDE.md',
        'source_location': None, 'source_url': None, 'captured_at': None, 'author': None, 'contributor': None
    })

# Fix source_file for images
for n in semantic['nodes']:
    if n['id'] == 'screenshot_explorer_overlay':
        n['source_file'] = base_root + '\\screenshots\\explorer.png'
    elif n['id'] == 'screenshot_visual_studio_overlay':
        n['source_file'] = base_root + '\\screenshots\\visual-studio.png'

edges = [
    # README: Hunt and Peck uses UI Automation navigation
    ('readme_hunt_and_peck', 'readme_ui_automation_navigation', 'references', 'EXTRACTED', 1.0),
    # Screenshots reference the project readme
    ('screenshot_explorer_overlay', 'readme_hunt_and_peck', 'cites', 'EXTRACTED', 1.0),
    ('screenshot_visual_studio_overlay', 'readme_hunt_and_peck', 'cites', 'EXTRACTED', 1.0),
    # Changelog: v1.1 includes interaction modes
    ('changelog_v1_1_elevated_modes', 'changelog_interaction_modes', 'references', 'EXTRACTED', 1.0),
    # Cross-doc: both README and CHANGELOG mention the fork of zsims/hunt-and-peck
    ('readme_hunt_and_peck', 'changelog_v1_1_elevated_modes', 'conceptually_related_to', 'INFERRED', 0.85),
    # CLAUDE.md references the project (graphify config)
    ('claude_graphify_config', 'readme_hunt_and_peck', 'references', 'EXTRACTED', 1.0),
]

for src, tgt, rel, conf, score in edges:
    semantic['edges'].append({
        'source': src, 'target': tgt, 'relation': rel, 'confidence': conf,
        'confidence_score': score, 'source_file': base_root + '\\README.md',
        'source_location': None, 'weight': 1.0
    })

# Fix source_file for changelog and clauded edges
for e in semantic['edges']:
    if any(x in (e.get('source','') or '') + str(e.get('target','')) for x in ['changelog']):
        e['source_file'] = base_root + '\\CHANGELOG.md'
    if 'claude' in (e.get('source','') or ''):
        e['source_file'] = base_root + '\\CLAUDE.md'

semantic['input_tokens'] = 0
semantic['output_tokens'] = 0

Path('graphify-out/.graphify_semantic.json').write_text(json.dumps(semantic, indent=2, ensure_ascii=False), encoding='utf-8')
print(f'Semantic: {len(semantic["nodes"])} nodes, {len(semantic["edges"])} edges')

# Merge AST + semantic
seen = {n['id'] for n in ast['nodes']}
merged_nodes = list(ast['nodes'])
for n in semantic['nodes']:
    if n['id'] not in seen:
        merged_nodes.append(n)
        seen.add(n['id'])

merged_edges = ast['edges'] + semantic['edges']
merged = {
    'nodes': merged_nodes,
    'edges': merged_edges,
    'hyperedges': [],
    'input_tokens': 0,
    'output_tokens': 0,
}
Path('graphify-out/.graphify_extract.json').write_text(json.dumps(merged, indent=2, ensure_ascii=False), encoding='utf-8')
print(f'Merged: {len(merged_nodes)} nodes, {len(merged_edges)} edges')
