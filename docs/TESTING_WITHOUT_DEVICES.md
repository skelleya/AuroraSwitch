# Testing Without Secondary Computers

## Overview

You can test the KVM Software Switch with only the host computer connected. The system will gracefully handle missing endpoints and show appropriate messages.

## Test Scenario: Host Only

### Expected Behavior

1. **On Launch**
   - Only "Host PC" endpoint appears in the list
   - Status shows "Active: Host"
   - Video preview shows "Select an endpoint to view video feed"

2. **Hotkey Behavior**
   - **Ctrl+F1**: Switches to host (already active, no change)
   - **Ctrl+F2+**: Shows "Endpoint not found" message
   - Video preview updates to show "Endpoint not found - This endpoint is not available"

3. **UI Display**
   - Endpoint list shows only Host PC
   - Status bar shows "Active: Host"
   - Video area shows placeholder text

## Test Steps

### 1. Launch Application
```
1. Run KvmSwitch.Dashboard.exe as Administrator
2. Application should launch successfully
3. Only "Host PC" should appear in endpoint list
```

### 2. Test Hotkey Switching
```
1. Press Ctrl+F1
   - Should show "Connected to host" (already active)
   - No error messages

2. Press Ctrl+F2
   - Status should show "Endpoint [id] not found"
   - Video preview should show:
     "Endpoint not found
     
     This endpoint is not available.
     Please refresh devices."

3. Press Ctrl+F1 again
   - Should return to host
   - Video preview should show:
     "Host PC
     
     Select an endpoint to view video feed"
```

### 3. Test Endpoint Selection
```
1. Click on "Host PC" in the list
   - Should select host endpoint
   - Status shows "Connected to host"
   - Video preview shows placeholder

2. Try to select a non-existent endpoint (if any appear)
   - Should show "Device not connected" message
   - Video preview shows appropriate error message
```

### 4. Test Device Refresh
```
1. Click "Refresh Devices" in menu
   - Should scan for devices
   - Only Host PC should appear (no secondary computers)
   - Status should update
```

## Expected Messages

### When Switching to Non-Existent Endpoint
- **Status Bar**: "Endpoint [id] not found"
- **Video Preview**: 
  ```
  Endpoint not found
  
  This endpoint is not available.
  Please refresh devices.
  ```

### When Selecting Disconnected Endpoint
- **Status Bar**: "[Endpoint Name] is not connected"
- **Video Preview**:
  ```
  [Endpoint Name]
  
  Device not connected
  
  Please connect the device and refresh
  ```

### When No Video Capture Device
- **Status Bar**: "[Endpoint Name] - No video capture device configured"
- **Video Preview**:
  ```
  [Endpoint Name]
  
  No video capture device configured
  or device not connected
  ```

## Verification Checklist

- [ ] Application launches without errors
- [ ] Only Host PC appears in endpoint list
- [ ] Ctrl+F1 works (switches to host)
- [ ] Ctrl+F2 shows "Endpoint not found" message
- [ ] Video preview shows appropriate placeholder text
- [ ] Status bar updates correctly
- [ ] No crashes or exceptions
- [ ] Refresh Devices works without errors

## Troubleshooting

### Issue: Application Crashes on Launch
- **Solution**: Ensure running as Administrator
- Check Windows Event Viewer for errors

### Issue: Hotkeys Not Working
- **Solution**: Verify application has administrator privileges
- Check if hotkeys conflict with other applications

### Issue: Endpoint List is Empty
- **Solution**: Host endpoint should be created automatically
- Check application logs for errors
- Try clicking "Refresh Devices"

### Issue: Error Messages Not Displaying
- **Solution**: Check that VideoPlaceholderText is bound correctly
- Verify StatusText updates are working

## Next Steps After Testing

Once testing without devices is successful:

1. **Connect Secondary Computer**
   - Connect HP Laptop via USB-C
   - Click "Refresh Devices"
   - Endpoint should appear in list

2. **Test with Real Device**
   - Press Ctrl+F2 to switch to HP Laptop
   - Verify input routing works
   - Test video capture (if HDMI capture device connected)

3. **Test MacBook Air**
   - Connect MacBook Air M2 via USB-C
   - Refresh devices
   - Test switching and input routing

## Notes

- The system is designed to gracefully handle missing endpoints
- All error states show user-friendly messages
- No crashes should occur when endpoints are missing
- Hotkeys for non-existent endpoints are ignored with a message

