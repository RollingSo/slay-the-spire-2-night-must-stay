from pathlib import Path
import cv2
import numpy as np

video_path = Path(r"D:\sts-2-mod\design\references\revenant_official_character_card.webm")
output_path = Path(r"D:\sts-2-mod\design\references\revenant_official_family_contact_sheet.png")

capture = cv2.VideoCapture(str(video_path))
fps = capture.get(cv2.CAP_PROP_FPS)
frame_count = int(capture.get(cv2.CAP_PROP_FRAME_COUNT))
duration = frame_count / fps

sample_times = np.linspace(0.0, max(0.0, duration - 0.15), 20)
frames = []
for seconds in sample_times:
    capture.set(cv2.CAP_PROP_POS_MSEC, float(seconds * 1000.0))
    ok, frame = capture.read()
    if not ok:
        continue
    frame = cv2.resize(frame, (320, 180), interpolation=cv2.INTER_AREA)
    cv2.putText(
        frame,
        f"{seconds:04.1f}s",
        (10, 24),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.6,
        (255, 255, 255),
        2,
        cv2.LINE_AA,
    )
    frames.append(frame)
capture.release()

while len(frames) < 20:
    frames.append(np.zeros((180, 320, 3), dtype=np.uint8))

rows = [np.hstack(frames[index:index + 5]) for index in range(0, 20, 5)]
sheet = np.vstack(rows)
cv2.imwrite(str(output_path), sheet)
print(output_path)

action_times = list(np.arange(10.8, 15.41, 0.2))
capture = cv2.VideoCapture(str(video_path))
action_frames = []
for seconds in action_times:
    capture.set(cv2.CAP_PROP_POS_MSEC, float(seconds * 1000.0))
    ok, frame = capture.read()
    if not ok:
        continue
    frame = cv2.resize(frame, (640, 360), interpolation=cv2.INTER_CUBIC)
    cv2.putText(frame, f"{seconds:04.1f}s", (12, 32), cv2.FONT_HERSHEY_SIMPLEX, 0.8,
                (255, 255, 255), 2, cv2.LINE_AA)
    action_frames.append(frame)
capture.release()

while len(action_frames) % 4:
    action_frames.append(np.zeros((360, 640, 3), dtype=np.uint8))
action_rows = [np.hstack(action_frames[index:index + 4])
               for index in range(0, len(action_frames), 4)]
action_sheet = np.vstack(action_rows)
action_path = output_path.with_name("revenant_official_family_action_contact_sheet.png")
cv2.imwrite(str(action_path), action_sheet)
print(action_path)

detail_times = list(np.arange(14.15, 15.31, 0.06))
capture = cv2.VideoCapture(str(video_path))
details = []
for seconds in detail_times:
    capture.set(cv2.CAP_PROP_POS_MSEC, float(seconds * 1000.0))
    ok, frame = capture.read()
    if not ok:
        continue
    crop = frame[175:670, 500:1130]
    crop = cv2.resize(crop, (504, 396), interpolation=cv2.INTER_CUBIC)
    cv2.putText(crop, f"{seconds:05.2f}s", (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.75,
                (255, 255, 255), 2, cv2.LINE_AA)
    details.append(crop)
capture.release()
while len(details) % 4:
    details.append(np.zeros((396, 504, 3), dtype=np.uint8))
detail_rows = [np.hstack(details[index:index + 4]) for index in range(0, len(details), 4)]
detail_sheet = np.vstack(detail_rows)
detail_path = output_path.with_name("revenant_official_family_detail_contact_sheet.png")
cv2.imwrite(str(detail_path), detail_sheet)
print(detail_path)
