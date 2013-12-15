#version 400
in vec3 p;

in vec3 lp1;
in vec3 lp2;
in vec3 normal;
noperspective in vec3 dist;
uniform int mode;
vec4 filled() {
	//basic material properties
	float exponent = 100.4;
	vec3 kd = vec3(0.5, 0.6, 0.4);
	vec3 ks = vec3(0.5, 0.5, 0.5);

	//scenewide ambient value
	vec3 amb = vec3(0, 0.01, 0.01);

	//normalizing factor
	float phys = (exponent + 8) / (8 * 3.14159);

	//light intensities
	vec3 l1 = vec3(1, 1, 1);
	vec3 l2 = vec3(0, 0, 0);

	//eye vector
	vec3 eye = normalize(-p);

	//calculate light vectors
	vec3 lv1 = normalize(l1 - p);
	vec3 lv2 = normalize(l2 - p);
	
	//half angles
	vec3 h1 = normalize(l1 + eye);
	vec3 h2 = normalize(l2 + eye);

	//diffuse contributions
	float d1 = clamp(dot(normal, l1), 0, 1);
	float d2 = clamp(dot(normal, l2), 0, 1);

	//specular contributions
	float s1 = pow(clamp(dot(normal, h1), 0, 1), exponent);
	float s2 = pow(clamp(dot(normal, h2), 0, 1), exponent);

	//contributions for each light
	vec3 c1 = d1 * (l1 * kd + l1 * ks * s1 * phys);
	vec3 c2 = d2 * (l2 * kd + l2 * ks * s2 * phys);

	//find total
	vec3 color = c1 + c2 + amb * kd;

	//correct for display gamma of 2.2
	return vec4 (pow(color, vec3(1 / 2.2)), 1.0);
}


vec4 wired() {
	//want to draw solid wireframe based on distance to nearest edge
	float d = min(dist.x, dist.y);
	d = min(d, dist.z);
	float i = 1 - pow(2, (-2 * pow(d, 2)));
	return vec4(i, i, i, 1);	
}

void main () {

	if(mode == 0)
		gl_FragColor = filled();
	else if(mode == 1)
		gl_FragColor = vec4(0.5, 0.7, 0.4, 1) * wired();
	else
		gl_FragColor = filled() * wired();
}
