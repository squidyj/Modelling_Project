#version 400

in vec3 vert0;
in vec3 vert1;
in vec3 vert2;

out vec3 worldpos;

out vec3 tlp1;
out vec3 tlp2;

uniform mat4 projection;
uniform mat4 view;
uniform mat4 model;

void main () {

	vec4 ip0 = view * model * vec4(vert0, 1); 
	worldpos = ip0.xyz;

	tlp1 = vec3 (0, 5, 5);
	tlp1 = (view * vec4(tlp1, 1)).xyz;

	tlp2 = vec3 (0, -5, -5);
	tlp2 = (view * vec4(tlp2, 1)).xyz;

	gl_Position = projection * ip0;
}