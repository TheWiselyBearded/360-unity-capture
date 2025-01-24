import os
from spatialmedia import metadata_utils

def inject_360_metadata(input_file, output_file, stereo_mode=None, spherical_only=False, spatial_audio=False):
    """
    Inject 360 video metadata using Google's Spatial Media Metadata Injector backend.

    Args:
        input_file (str): Path to the input MP4/MOV video file.
        output_file (str): Path to save the output video with injected metadata.
        stereo_mode (str): Stereo mode for the video, e.g., "top-bottom". Default is None.
        spherical_only (bool): If True, the video is treated as spherical 360 without stereo.
        spatial_audio (bool): Whether the video includes spatial audio metadata.
    """
    if not os.path.exists(input_file):
        raise FileNotFoundError(f"Input file not found: {input_file}")
    
    # Generate video metadata
    metadata = metadata_utils.Metadata()
    
    if spherical_only:
        # Only spherical metadata, no stereo
        metadata.video = metadata_utils.generate_spherical_xml(stereo=None)
    else:
        # Add stereo metadata if specified
        metadata.video = metadata_utils.generate_spherical_xml(stereo=stereo_mode)

    # Add spatial audio metadata if requested
    if spatial_audio:
        # Placeholder values for spatial audio description (can be adjusted as needed)
        audio_description = metadata_utils.AudioMetadataDescription(order=1, has_head_locked_stereo=False)
        metadata.audio = metadata_utils.get_spatial_audio_metadata(
            audio_description.order, audio_description.has_head_locked_stereo
        )

    # Inject metadata into the video
    console_log = []
    metadata_utils.inject_metadata(input_file, output_file, metadata, console_log.append)

    # Print log messages
    for log in console_log:
        print(log)
    print(f"Metadata injected successfully into: {output_file}")


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Inject 360 video metadata into MP4/MOV files.")
    parser.add_argument("input", help="Path to the input video file (MP4/MOV).")
    parser.add_argument("output", help="Path to save the output video with injected metadata.")
    parser.add_argument("--stereo", choices=["top-bottom", "left-right"], help="Stereo mode (e.g., 'top-bottom').")
    parser.add_argument("--spherical-only", action="store_true", help="Enable only spherical 360 metadata (no stereo).")
    parser.add_argument("--spatial-audio", action="store_true", help="Enable spatial audio metadata.")

    args = parser.parse_args()

    try:
        inject_360_metadata(
            args.input,
            args.output,
            stereo_mode=args.stereo,
            spherical_only=args.spherical_only,
            spatial_audio=args.spatial_audio
        )
    except Exception as e:
        print(f"Error: {e}")
