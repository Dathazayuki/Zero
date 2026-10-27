# Cyberpunk City — starry night sky

`Cyberpunk_StarryNight_Panorama.png` is an AI-generated LDR panorama, **1774 × 887**
(2:1), generated with the built-in image generation tool. It is not an HDRI or a
native 4K asset. Original generation is retained outside the project; this local
copy is the asset consumed by Unity.

Material: `Cyberpunk_StarryNight.mat`, shader `Skybox/Panoramic`, latitude/longitude
mapping, 360°, no stereo layout, exposure 1, neutral tint 0.5, rotation 0°.
Texture U repeats, V clamps, mipmaps/trilinear enabled.

Scene setup uses blue moonlight intensity 0.18, subdued three-color ambient lighting
and skybox reflections at 0.35. Existing neon emission, materials, geometry, collider
components and map scale are unchanged. Existing fog is recolored only if enabled;
fog is not enabled automatically. Scene cameras use Skybox clear flags.

Changes are authored once through an explicit menu, not a startup/runtime script.
Save the scene manually with Ctrl+S. To adjust brightness later, edit the skybox
material Exposure and the Directional Light intensity in the scene.

## Generation prompt

Use case: photorealistic-natural. Asset type: production Unity Skybox/Panoramic texture, true 360-degree equirectangular latitude-longitude projection, full sphere, exact 2:1 wide aspect ratio, highest available resolution preferably 4096x2048. Generate a clean beautiful realistic clear star-filled night sky for a dark cyberpunk city. Deep midnight navy-black sky with thousands of tiny sharp varied white and subtly cool-blue stars, natural nonuniform star distribution, a delicate dim Milky Way dust band across the upper hemisphere, very faint blue-violet atmospheric glow close to the equator/horizon only. Must remain genuinely nighttime, not bright fantasy space. Seamless left/right wrap with matching brightness and star density at the horizontal edges; correct polar compression for spherical mapping, no radial starburst artifacts. Zenith near top; horizon at vertical midpoint; lower hemisphere gently fades to dark navy-black, no ground. Pure sky only: absolutely no buildings, city silhouette, terrain, trees, landscape, people, text, moon, sun, planets, water or watermark. No huge stars, no lens flare, no hard horizon line, no bright colorful nebula. This is a sky texture, NOT a photo of a framed sky, NOT a cubemap cross, NOT six panels.
