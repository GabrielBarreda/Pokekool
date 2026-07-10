"""
Script companion Blender — exporte la scène chargée en .glb (binary glTF).

Invoqué par le backend via :
    blender --background "{blend}" --python "scripts/blender/export_glb.py" -- "{output.glb}"

Le fichier .blend est déjà chargé par Blender (option --background "{blend}")
avant l'exécution de ce script. Le chemin de sortie est passé après le séparateur "--".
"""

import bpy
import sys

argv = sys.argv[sys.argv.index("--") + 1:]
output_path = argv[0]

bpy.ops.export_scene.gltf(
    filepath=output_path,
    export_format='GLB',
    export_apply=True,
)
