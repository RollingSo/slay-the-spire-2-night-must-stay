"""Local color-only repair for the generated warm patch inside the claw skirt.
No geometry, alpha, bare lower legs, face or arms are changed here.
"""
import numpy as np
from scipy import ndimage as ndi

def correct(rgb):
 f=rgb.astype(np.float32);h,w=f.shape[:2];yy,xx=np.indices((h,w))
 # Excludes bare legs below the hem; hands are excluded by component extent.
 roi=(xx>=350)&(xx<=560)&(yy>=480)&(yy<=834)
 warm=(f[:,:,0]-f[:,:,1]>6)&(f[:,:,0]-f[:,:,2]>5)&(f[:,:,0]>120)&roi
 labels,count=ndi.label(warm)
 mask=np.zeros((h,w),bool)
 for k,sl in enumerate(ndi.find_objects(labels),1):
  if sl is None:continue
  part=labels[sl]==k
  # The unwanted skirt patch extends below the waist. Detached hands don't.
  if part.sum()>=700 and sl[0].stop>650 and sl[0].start<780:
   mask[sl]|=part
 mask=ndi.binary_fill_holes(mask)
 weight=ndi.gaussian_filter(mask.astype(np.float32),.7)*roi
 # Retain source luminance/shadows; match the cool grey-white fabric chroma.
 lum=.2126*f[:,:,0]+.7152*f[:,:,1]+.0722*f[:,:,2]
 target=np.stack([lum-2.5,lum,lum+4.5],axis=2)
 out=np.clip(f*(1-weight[:,:,None])+target*weight[:,:,None],0,255).astype(np.uint8)
 return out,weight
