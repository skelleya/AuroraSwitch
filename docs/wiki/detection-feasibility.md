# Detection & Multi-Monitor Feasibility

## Summary
- USB-C targets can be identified reliably using Windows SetupAPI and USB descriptor parsing; supports MacBook Air, HP laptops, and most modern devices.
- Fallback heuristics now treat Apple-branded adapters (manufacturer strings such as *Apple Inc.* or *MacBook Air*) as valid macOS endpoints even when vendor IDs are obscured by USB-C → USB-A dongles.
- HDMI-only targets require EDID interrogation through capture hardware plus PnP `DISPLAY` enumeration so Apple sources connected via HDMI are surfaced without manual tagging.
- Multi-monitor support for Apple Silicon MacBook Air is constrained by native GPU limitations; DisplayLink-based docks provide the most viable workaround.

## USB-C Detection
- Query `GUID_DEVINTERFACE_USB_DEVICE` via SetupDi to enumerate Type-C ports and attached peripherals.
- Parse USB descriptors:
  - **Vendor/ Product IDs** to infer manufacturer (e.g., Apple `05AC`, HP `03F0`).
  - **BOS descriptors** to detect alternate mode support (DisplayPort, Thunderbolt).
  - **USB Billboard class** to understand failed alt-mode negotiations.
- Use manufacturer strings as a secondary signal—if `Manufacturer` or `Name` contains Apple/MacBook keywords, force the endpoint type to macOS even when the adapter masks the original vendor ID.
- Leverage Windows USB Type-C Connector System Software (UCSI) notifications for hot-plug events without polling.
- Maintain cache of known devices in `endpoints.db` with friendly names and capability flags (`SupportsAltMode`, `SupportsPD`, `HasHIDUpstream`).
- Limitation: when using passive USB-C to HDMI adapters, HID upstream information is unavailable; rely on manual classification.

## HDMI Detection
- Capture cards expose EDID data via DirectShow `IAMVideoProcAmp` or vendor extensions.
- Parse EDID base block to extract manufacturer ID, product code, and preferred timing (resolution, refresh).
- Enumerate `Win32_PnPEntity` entries with `PNPClass = 'DISPLAY'` to surface Apple display descriptors (e.g., `DISPLAY\APP*`) as macOS HDMI endpoints even without EDID access.
- Identify host by comparing EDID serial numbers against known device list (if user pre-registers).
- Challenges:
  - Many consumer capture devices report the capture card manufacturer (e.g., `SUB 1E4 AJA`) instead of the downstream laptop.
  - HDMI splitters and switches may mutate EDID, masking the original source.
  - No standard path to detect operating system or device type solely from EDID.
- Mitigations:
  - Allow users to tag inputs manually.
  - Combine EDID with USB upstream presence (if capture device includes USB hub for HID).
  - Provide optional LAN agent to advertise identity over mDNS.

## Audio Routing Considerations
- USB-C devices often expose integrated audio endpoints (DisplayPort Alt Mode with audio). Confirm support via USB interface descriptors.
- HDMI capture cards typically surface PCM stereo audio; multi-channel support depends on hardware (Elgato 4K60 Pro supports 7.1 pass-through but 2.0 capture).
- WASAPI loopback enables host audio capture when capture card only delivers video; requires HDMI audio to be fed into host GPU or AVR.

## MacBook Air Multi-Monitor Analysis
- Apple Silicon MacBook Air models natively support one external display at up to 6K60 (Thunderbolt/USB4).
- MST hubs do not enable additional displays because macOS disables MST for display expansion.
- DisplayLink or SiliconMotion-based docks virtualize additional displays using USB compression; requires macOS driver installation and host CPU/GPU resources.
- HDMI capture solution options:
  1. **Native single display**: route Mac HDMI/USB-C output to dual-input capture (mirrored on both monitors).
  2. **DisplayLink dock**: connect dock to Mac, use host capture for each DisplayLink output (requires driver, adds 150–200 ms latency).
  3. **Sidecar/Stage Manager**: leverage iPad or host Mac as additional displays; limited use for external KVM scenario.
- Recommendation: standardize on DisplayLink-certified dock (e.g., Plugable UD-6950H) for multi-monitor setups; document driver install steps in WIKI.

## Risk Assessment
- **Detection Accuracy**: High for USB-C; medium for HDMI due to EDID ambiguity.
- **Driver Dependencies**: DisplayLink drivers must be kept updated; licensing prohibits redistribution without agreement.
- **Latency Impact**: USB-over-IP adds ~8 ms per direction; DisplayLink adds up to 40 ms; acceptable for productivity, marginal for gaming.
- **User Experience**: Provide clear UI indicators when detection confidence is low and prompt for manual labeling.

## Next Actions
- Implement USB descriptor parser and integrate with `HostControlService` discovery loop.
- Prototype EDID reader against target capture hardware (Magewell, Elgato) to gauge accuracy.
- Evaluate DisplayLink SDK licensing; prepare documentation for optional multi-monitor support.


