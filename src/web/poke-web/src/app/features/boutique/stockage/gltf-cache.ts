import * as THREE from 'three';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';

/**
 * Cache de chargement glTF partagé (module-level) : chaque URL n'est téléchargée/parsée
 * qu'une fois, puis clonée à chaque usage. Adapté aux assets fixes (cartons, classeurs).
 */
const loader = new GLTFLoader();
const cache = new Map<string, Promise<THREE.Object3D>>();

export function loadGltfClone(url: string): Promise<THREE.Object3D> {
  if (!cache.has(url)) {
    cache.set(
      url,
      new Promise<THREE.Object3D>((resolve, reject) => {
        loader.load(url, gltf => resolve(gltf.scene), undefined, reject);
      }),
    );
  }
  // clone(true) : partage géométries/matériaux (assets statiques), duplique le graphe.
  return cache.get(url)!.then(scene => scene.clone(true));
}
