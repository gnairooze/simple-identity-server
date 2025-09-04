# Test script to verify token masking is working correctly
# This script demonstrates the before/after token masking behavior

Write-Host "Testing Token Masking Patterns" -ForegroundColor Green
Write-Host "==============================" -ForegroundColor Green

# Simulate the problematic log entry you observed
$problematicContent = "grant_type=client_credentials&client_id=service-api&client_secret=your-secret&token=CfDJ8Nt5Kv2QqW8.4oCQdxwor6U3oJl_Rc3QMw.Y_l2xfG3WyqmFeeHydO6ZTTkrZzw4RxWl93797D_WS0ygQqcoXIfDdhzFazcrspj-hTRAkXJA9YADpKf40RcJkI63FedUvLf0CUGmbvgrrbE7c3_k0mLuOOh4AUY87EQzzsooxNXfQZq_2LwAy4qB8Ut52P5JWdUxeoth9ijeD-sMRbnwvZw-WQB9P2jfCVlEhTwG0DS1OTMpPbm_5WfJ4WPWQjZA3A1Yz9vouFt3rDFrFSGXeGGXJPFRpNaEhv_M2AE_buYrDNHR_NsHu5bJ8-EBWuXc9JxZhCnCTkrZ-C-73Pz_XcbaPnwPhG0n3kxYvLdwOKAue8czZOXkmKohOWtuaOcqnc2qkHVg4FTAd1x8qAwEN60DvVqXYCGhSgjMK25HVPp2i2xR_nbEOkiO0haSRhN-R_yDwX0bKbVsiToe8Vx-h_Fd3HWPrBHwpXA26iXNWmDrBf9B-igPAyuaNJ4_GvRVcfH6c3RPmJbGxjX9uy8Yrku73rPCDZgrRxfj11cocAqrMUd75oxA8AHW0bLaqWDzlGeAO6jBHwiGXwu5SHjkB_LWhnRG_-M5hJd1QI6gV3Cx_U42pUNiQH0-M-abc123-15bENph_LhCiOS25DKtzN1unVcmUnJcH6VemXeR8Pbm1lRYzwN3Pp8nebGQm91TW6J5I3wAnhRQrQqXgZ9j03x1WdoPfA8xxuv1sMaODgC25bNAwq_C0HMHqVweK5fojyl5UTjx4mJD__mHd_UbC7rGaq2JHSiLVbLOu6wr5HXNVZ2IvLSfODJ7bSLGytPlm0qtDh-5mWlkcW20t3K60VuvZtgZ5CzIU4JOixI_fqzcVqZtMLdOmXYmoJgKYxIILDak-dCU5eRkzXRfm4X1AN64WKFH5PodC_o929koerVKTSVvtlYO5DIpMXAEkZM7QE8ojZeEByiZ_OKYjPBWv-DLMiEZvuuRNA1TWI_JhBvJ-gdh0JvZwvBp8v0VYQq-def456.E5sHZ9IsTWUEzRiFo_UnzXGF8RVJ4CfW39INZPi1OME"

Write-Host "`nOriginal problematic content:" -ForegroundColor Yellow
Write-Host $problematicContent.Substring(0, [Math]::Min(100, $problematicContent.Length)) + "..." -ForegroundColor Red

# Simulate the old masking behavior (partial masking)
Write-Host "`nOLD Behavior (partial masking):" -ForegroundColor Yellow
$oldMasked = $problematicContent -replace '\b[A-Za-z0-9]{20,}\b', '[MASKED_KEY]'
$oldMasked = $oldMasked -replace 'client_secret=[^&\s]*', 'client_secret=[MASKED]'
Write-Host $oldMasked.Substring(0, [Math]::Min(150, $oldMasked.Length)) + "..." -ForegroundColor Red

# Simulate the new masking behavior (complete token masking)
Write-Host "`nNEW Behavior (complete token masking):" -ForegroundColor Yellow
$newMasked = $problematicContent
# Apply the new patterns in order (most specific first)
$newMasked = $newMasked -replace 'token=[^&\s]*', 'token=[MASKED]'
$newMasked = $newMasked -replace 'client_secret=[^&\s]*', 'client_secret=[MASKED]'
$newMasked = $newMasked -replace 'password=[^&\s]*', 'password=[MASKED]'
Write-Host $newMasked -ForegroundColor Green

Write-Host "`n==============================" -ForegroundColor Green
Write-Host "Analysis:" -ForegroundColor Green
Write-Host "✓ OLD: Token was partially visible with structure exposed" -ForegroundColor Red
Write-Host "✓ NEW: Token is completely masked for security" -ForegroundColor Green
Write-Host "✓ Client secret remains properly masked in both cases" -ForegroundColor Green

Write-Host "`nSecurity Improvement:" -ForegroundColor Yellow
Write-Host "- No token structure or content is revealed in logs" -ForegroundColor Cyan
Write-Host "- Encrypted token format is completely hidden" -ForegroundColor Cyan
Write-Host "- Maintains debugging capability without security risk" -ForegroundColor Cyan

# Test other token formats
Write-Host "`nTesting other token formats:" -ForegroundColor Yellow

$jwtToken = "Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWV9.EkN-DOsnsuRjRO6BxXemmJDm3HbxrbRzXglbN2S4sOkopdU4IsDxTI8jO19W_A4K8ZPJijNLis4EZsHeY559a4DFOd50_OqgHs3PH1GQlhz6iD8XKLG8HXo4aWKHq0gVl8"
$maskedJwt = $jwtToken -replace 'Bearer\s+[A-Za-z0-9\-._~+/]+=*', 'Bearer [MASKED]'
Write-Host "JWT: $maskedJwt" -ForegroundColor Green

$formWithToken = "access_token=abc123def456&refresh_token=xyz789uvw012&grant_type=refresh_token"
$maskedForm = $formWithToken -replace 'token=[^&\s]*', 'token=[MASKED]'
Write-Host "Form data: $maskedForm" -ForegroundColor Green

Write-Host "`nToken masking fix is ready! 🔒" -ForegroundColor Green
