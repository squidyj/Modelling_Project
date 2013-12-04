#version 400
in vec3 fpos;
in vec3 fnorm;
in vec3 lpos;
in vec2 tex;
void main () {

	float exponent = 100.4;
	vec3 amb = vec3(0, 0.01, 0.01);
	vec3 normal = normalize(fnorm);
	vec3 lvec = normalize(lpos - fpos);
	vec3 evec = normalize(-fpos);
	vec3 hlf = normalize(evec + lvec);
	float dif = clamp(dot(fnorm, lvec), 0, 1);
	float spec = pow(clamp(dot(fnorm, hlf), 0, 1), exponent);
	vec3 kd = vec3(0.5, 0.6, 0.4);
	vec3 ks = vec3(0.5, 0.5, 0.5);
	vec3 ld = vec3(1, 1, 1);
	float phys = (exponent + 8) / (8 * 3.14159);
	vec3 color = dif * (ld * kd + ld * ks * spec * phys) + amb * kd;
	gl_FragColor = vec4 (pow(color, vec3(1 / 2.2)), 1.0);
}