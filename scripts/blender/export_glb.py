"""
Script companion Blender — exporte la scène chargée en .glb (binary glTF).

Invoqué par le backend via :
    blender --background "{blend}" --python "scripts/blender/export_glb.py" -- "{output.glb}"

Le fichier .blend est déjà chargé par Blender (option --background "{blend}")
avant l'exécution de ce script. Le chemin de sortie est passé après le séparateur "--".
"""

import bpy
import sys
import traceback


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    output_path = argv[0]

    # Certains builds Blender headless (Docker) n'activent pas l'exporteur glTF par défaut.
    try:
        bpy.ops.preferences.addon_enable(module="io_scene_gltf2")
    except Exception:
        pass  # déjà actif / intégré

    bpy.ops.export_scene.gltf(
        filepath=output_path,
        export_format='GLB',
        export_apply=True,
    )


try:
    main()
except Exception:
    # Trace complète sur stderr → remontée telle quelle dans le badge de l'UI.
    traceback.print_exc()
    sys.exit(1)
