# Testing Matrix and Procedures

## Test Matrix Overview

This document defines the test procedures for validating KVM Software Switch functionality, performance, and compatibility.

## Test Categories

### 1. Device Discovery and Detection

#### Test Case: USB-C Device Detection
**Objective**: Verify that USB-C connected devices are properly detected and identified.

**Prerequisites**:
- HP Laptop connected via USB-C
- MacBook Air M2 connected via USB-C
- Host PC running KVM Software Switch

**Procedure**:
1. Launch KVM Software Switch application
2. Click "Refresh Devices" in the menu
3. Verify both HP Laptop and MacBook Air appear in endpoint list
4. Verify device types are correctly identified (Windows vs macOS)
5. Verify connection type shows as "USB-C"

**Expected Results**:
- Both devices appear in endpoint list within 5 seconds
- Device types are correctly identified
- Vendor IDs are detected (HP: 03F0, Apple: 05AC)

**Pass Criteria**: All devices detected and correctly identified

---

#### Test Case: HDMI Capture Device Detection
**Objective**: Verify that HDMI capture cards are detected and associated with endpoints.

**Prerequisites**:
- HDMI capture card(s) connected to host PC
- Secondary machines connected to capture cards via HDMI

**Procedure**:
1. Connect HDMI capture card to host PC
2. Connect secondary machine to capture card via HDMI
3. Launch application and refresh devices
4. Verify capture device appears in system
5. Verify capture device is associated with correct endpoint

**Expected Results**:
- Capture device detected via DirectShow enumeration
- Capture device ID stored in endpoint metadata
- Video preview available when endpoint is selected

**Pass Criteria**: Capture devices detected and mapped to endpoints

---

### 2. Hotkey Switching

#### Test Case: Host to Secondary Machine Switch
**Objective**: Verify that Ctrl+F2 successfully switches control to secondary machine.

**Prerequisites**:
- HP Laptop configured with hotkey Ctrl+F2
- Host PC currently active

**Procedure**:
1. Ensure host PC is active (mouse/keyboard control on host)
2. Press Ctrl+F2
3. Verify mouse/keyboard control transfers to HP Laptop
4. Verify host PC no longer receives input
5. Verify status bar shows "Active: [HP Laptop ID]"

**Expected Results**:
- Switch completes within 100ms
- Mouse/keyboard input goes to HP Laptop only
- Host PC is locked out from input
- Status updates correctly

**Pass Criteria**: Successful switch with exclusive control

---

#### Test Case: Secondary Machine to Host Switch
**Objective**: Verify that Ctrl+F1 returns control to host.

**Prerequisites**:
- HP Laptop currently active
- Host hotkey configured as Ctrl+F1

**Procedure**:
1. Ensure HP Laptop is active
2. Press Ctrl+F1
3. Verify control returns to host PC
4. Verify HP Laptop is locked out
5. Verify status bar shows "Active: Host"

**Expected Results**:
- Switch completes within 100ms
- Control returns to host
- Secondary machine locked out
- Status updates correctly

**Pass Criteria**: Successful return to host

---

#### Test Case: Hotkey Reliability (Emergency Recovery)
**Objective**: Verify that emergency hotkey (Ctrl+Shift+F12) always returns control to host.

**Prerequisites**:
- Any endpoint active (including unresponsive ones)

**Procedure**:
1. Activate any secondary machine
2. Simulate unresponsive state (disconnect network if USB/IP)
3. Press Ctrl+Shift+F12
4. Verify control returns to host within 500ms
5. Verify all endpoints are unlocked

**Expected Results**:
- Emergency hotkey always works
- Control returns to host even if endpoint is unresponsive
- All sessions are properly cleaned up

**Pass Criteria**: Emergency recovery successful

---

### 3. Peripheral Routing and Exclusive Control

#### Test Case: Exclusive Keyboard Control
**Objective**: Verify that only the active endpoint receives keyboard input.

**Prerequisites**:
- Host PC, HP Laptop, and MacBook Air all connected
- Host PC currently active

**Procedure**:
1. Activate HP Laptop endpoint
2. Type text in Notepad on host PC (should not appear)
3. Type text in Notepad on HP Laptop (should appear)
4. Type text in TextEdit on MacBook Air (should not appear)
5. Switch to MacBook Air
6. Type text in TextEdit on MacBook Air (should appear)
7. Type text on HP Laptop (should not appear)

**Expected Results**:
- Only active endpoint receives keyboard input
- Other endpoints are completely locked out
- No input leakage between endpoints

**Pass Criteria**: 100% exclusive control verified

---

#### Test Case: Exclusive Mouse Control
**Objective**: Verify that only the active endpoint receives mouse input.

**Prerequisites**:
- Multiple endpoints connected

**Procedure**:
1. Activate HP Laptop endpoint
2. Move mouse - verify cursor moves on HP Laptop only
3. Click on host PC desktop - verify no response
4. Click on MacBook Air - verify no response
5. Switch to MacBook Air
6. Move mouse - verify cursor moves on MacBook Air only
7. Click on HP Laptop - verify no response

**Expected Results**:
- Mouse movement only affects active endpoint
- Clicks only register on active endpoint
- No mouse input leakage

**Pass Criteria**: Exclusive mouse control verified

---

### 4. Latency Testing

#### Test Case: Switching Latency
**Objective**: Measure time from hotkey press to endpoint activation.

**Prerequisites**:
- High-speed camera or latency measurement tool
- Multiple endpoints connected

**Procedure**:
1. Record screen of target endpoint
2. Press hotkey to switch to endpoint
3. Measure time from hotkey press to first input accepted
4. Repeat 10 times for each endpoint type (USB-C, USB/IP)
5. Calculate average, min, max latency

**Expected Results**:
- Average latency < 100ms for USB-C
- Average latency < 200ms for USB/IP over Gigabit LAN
- Maximum latency < 500ms

**Pass Criteria**: Latency within acceptable thresholds

---

#### Test Case: Input Latency (Mouse Movement)
**Objective**: Measure round-trip latency for mouse movement.

**Prerequisites**:
- Latency measurement tool
- High refresh rate monitor on target endpoint

**Procedure**:
1. Activate endpoint
2. Move mouse in circular pattern
3. Measure time from physical mouse movement to cursor movement on target
4. Repeat 50 times
5. Calculate average latency

**Expected Results**:
- USB-C: < 20ms average latency
- USB/IP: < 40ms average latency (Gigabit LAN)
- No noticeable lag during normal usage

**Pass Criteria**: Latency acceptable for normal use

---

### 5. Video Capture and Display

#### Test Case: Video Feed Display
**Objective**: Verify that video from capture devices displays correctly in UI.

**Prerequisites**:
- HDMI capture card connected
- Secondary machine connected to capture card

**Procedure**:
1. Select endpoint with capture device
2. Verify video feed appears in preview window
3. Verify video is smooth (no stuttering)
4. Verify video resolution matches capture device settings
5. Switch between endpoints - verify video switches correctly

**Expected Results**:
- Video displays within 1 second of selection
- Frame rate ≥ 30fps for 1080p
- No visual artifacts or corruption
- Smooth switching between feeds

**Pass Criteria**: Video display functional and smooth

---

#### Test Case: Multi-Monitor Support (MacBook Air)
**Objective**: Verify multi-monitor support with DisplayLink dock.

**Prerequisites**:
- MacBook Air M2 connected via DisplayLink dock
- DisplayLink Manager installed on MacBook Air
- Two capture devices connected to DisplayLink outputs

**Procedure**:
1. Detect MacBook Air with DisplayLink dock
2. Verify both capture devices are associated with MacBook Air endpoint
3. Select MacBook Air endpoint
4. Verify both video feeds display (if UI supports multi-view)
5. Verify both monitors are active on MacBook Air

**Expected Results**:
- DisplayLink dock detected
- Both capture devices mapped correctly
- Both monitors active on MacBook Air
- Video feeds display correctly

**Pass Criteria**: Multi-monitor configuration functional

---

### 6. Audio Routing

#### Test Case: Audio Output Routing
**Objective**: Verify that audio from capture devices routes to host speakers.

**Prerequisites**:
- Capture device with audio support
- Secondary machine playing audio

**Procedure**:
1. Select endpoint with audio-capable capture device
2. Play audio on secondary machine
3. Verify audio plays through host speakers
4. Switch to different endpoint
5. Verify audio switches correctly

**Expected Results**:
- Audio plays through host speakers
- Audio latency < 50ms
- No audio dropouts or distortion
- Audio switches smoothly between endpoints

**Pass Criteria**: Audio routing functional

---

### 7. Endpoint Compatibility

#### Test Case: HP Laptop Compatibility
**Objective**: Verify full functionality with HP Laptop.

**Prerequisites**:
- HP Laptop connected via USB-C

**Test Checklist**:
- [ ] Device detection
- [ ] Hotkey switching
- [ ] Keyboard input routing
- [ ] Mouse input routing
- [ ] Video capture (if HDMI capture used)
- [ ] Audio routing (if applicable)
- [ ] Exclusive control
- [ ] Switching latency < 100ms

**Pass Criteria**: All checklist items pass

---

#### Test Case: MacBook Air M2 Compatibility
**Objective**: Verify full functionality with MacBook Air M2.

**Prerequisites**:
- MacBook Air M2 connected via USB-C

**Test Checklist**:
- [ ] Device detection (Apple Vendor ID)
- [ ] Hotkey switching
- [ ] Keyboard input routing
- [ ] Mouse input routing
- [ ] Video capture (if HDMI capture used)
- [ ] Audio routing (if applicable)
- [ ] Exclusive control
- [ ] Multi-monitor support (if DisplayLink used)
- [ ] Switching latency < 100ms

**Pass Criteria**: All checklist items pass

---

### 8. Error Handling and Recovery

#### Test Case: Endpoint Disconnection Handling
**Objective**: Verify graceful handling when endpoint disconnects.

**Prerequisites**:
- Active endpoint connected

**Procedure**:
1. Activate endpoint
2. Disconnect USB-C cable or network connection
3. Verify application detects disconnection
4. Verify control automatically returns to host
5. Verify error message displayed
6. Reconnect endpoint
7. Verify endpoint reappears in list

**Expected Results**:
- Disconnection detected within 2 seconds
- Control returns to host automatically
- Error message displayed
- Reconnection works correctly

**Pass Criteria**: Graceful error handling

---

#### Test Case: Capture Device Failure
**Objective**: Verify handling when capture device fails.

**Prerequisites**:
- Endpoint with active video capture

**Procedure**:
1. Select endpoint with video capture
2. Disconnect capture device
3. Verify application handles failure gracefully
4. Verify error message displayed
5. Verify endpoint remains functional (peripheral routing still works)

**Expected Results**:
- Failure detected within 2 seconds
- Error message displayed
- Peripheral routing continues to work
- Video placeholder shown

**Pass Criteria**: Graceful degradation

---

## Performance Benchmarks

### Target Metrics

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| Switching Latency (USB-C) | < 100ms | High-speed camera |
| Switching Latency (USB/IP) | < 200ms | High-speed camera |
| Input Latency (USB-C) | < 20ms | Latency measurement tool |
| Input Latency (USB/IP) | < 40ms | Latency measurement tool |
| Video Frame Rate | ≥ 30fps @ 1080p | Frame counter |
| Audio Latency | < 50ms | Audio latency tool |
| CPU Usage (Idle) | < 5% | Task Manager |
| CPU Usage (Active) | < 15% | Task Manager |
| Memory Usage | < 200MB | Task Manager |

## Test Execution Log

### Test Session: [Date]

**Environment**:
- Host OS: Windows [Version]
- HP Laptop: [Model]
- MacBook Air: M2 [Year]
- Capture Devices: [List]
- Network: [Gigabit/WiFi]

**Results**:

| Test Case | Status | Notes | Latency (if applicable) |
|-----------|--------|-------|------------------------|
| USB-C Detection | Pass/Fail | | |
| Hotkey Switching | Pass/Fail | | [ms] |
| Exclusive Control | Pass/Fail | | |
| Video Display | Pass/Fail | | [fps] |
| Audio Routing | Pass/Fail | | [ms] |
| Multi-Monitor (Mac) | Pass/Fail | | |
| Error Handling | Pass/Fail | | |

**Issues Found**:
1. [Issue description]
   - Severity: [High/Medium/Low]
   - Status: [Open/Resolved]
   - Resolution: [If resolved]

**Performance Metrics**:
- Average Switching Latency: [ms]
- Average Input Latency: [ms]
- CPU Usage: [%]
- Memory Usage: [MB]

## Regression Testing

After each release, execute the following critical test cases:
1. Hotkey switching (all endpoints)
2. Exclusive control verification
3. Endpoint disconnection handling
4. Latency measurements
5. Compatibility with HP Laptop and MacBook Air

## Known Issues and Limitations

1. **DisplayLink Latency**: Multi-monitor via DisplayLink adds 20-50ms latency
2. **USB/IP Network Dependency**: Requires stable Gigabit LAN for acceptable latency
3. **macOS Multi-Monitor**: Limited to DisplayLink solution for M2 MacBook Air
4. **Capture Device Compatibility**: Some capture devices may not be detected correctly

