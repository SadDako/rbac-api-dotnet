$baseUrl = "http://localhost:5083"

$email = "daniel@email.com"
$password = "123456"

Write-Host "1) Fazendo login..."
$loginBody = @{
  email = $email
  password = $password
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod `
  -Method POST `
  -Uri "$baseUrl/auth/login" `
  -ContentType "application/json" `
  -Body $loginBody

$token = $loginResponse.accessToken

if ([string]::IsNullOrWhiteSpace($token)) {
  Write-Host "Falhou: token vazio. Resposta foi:"
  $loginResponse | ConvertTo-Json -Depth 10
  exit 1
}

Write-Host "Token recebido (primeiros 30 chars): $($token.Substring(0, [Math]::Min(30, $token.Length)))..."

Write-Host "2) Chamando /users/me..."
$meResponse = Invoke-RestMethod `
  -Method GET `
  -Uri "$baseUrl/users/me" `
  -Headers @{ Authorization = "Bearer $token" }

Write-Host "Resposta /users/me:"
$meResponse | ConvertTo-Json -Depth 10
