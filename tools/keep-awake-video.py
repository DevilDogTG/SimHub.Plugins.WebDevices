# Makes the keep-awake video embedded in src/DevDogs.WebDevices.Core/Server/Assets/device.html (the base64 data URI
# of the #awake video): 16x16 black, 5 s at 10 fps, H.264 in MP4, no audio track, about 2.6 KB.
# Uses Blender's bundled FFmpeg, so no other tools are needed:
#   blender -b --factory-startup --python tools/keep-awake-video.py -- keep-awake.mp4
# then paste the output of `base64 -w0 keep-awake.mp4` after "data:video/mp4;base64," in device.html.
import sys
import bpy

out = sys.argv[sys.argv.index("--") + 1]

scene = bpy.context.scene
scene.render.resolution_x = 16
scene.render.resolution_y = 16
scene.render.resolution_percentage = 100
scene.render.fps = 10
scene.render.fps_base = 1
scene.frame_start = 1
scene.frame_end = 50  # 5 s

if not scene.sequence_editor:
    scene.sequence_editor_create()
strip = scene.sequence_editor.sequences.new_effect(
    name="black", type='COLOR', channel=1, frame_start=1, frame_end=51)
strip.color = (0.0, 0.0, 0.0)
scene.render.use_sequencer = True
scene.render.use_compositing = False

settings = scene.render.image_settings
settings.file_format = 'FFMPEG'
settings.color_mode = 'RGB'
ff = scene.render.ffmpeg
ff.format = 'MPEG4'
ff.codec = 'H264'
ff.constant_rate_factor = 'LOWEST'
ff.ffmpeg_preset = 'BEST'
ff.gopsize = 50
ff.max_b_frames = 0
ff.audio_codec = 'NONE'

scene.render.filepath = out
scene.render.use_file_extension = False
bpy.ops.render.render(animation=True)
print("WROTE", out)
