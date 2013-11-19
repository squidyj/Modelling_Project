#version 400

in vec3 position;
in vec3 normal;
in vec3 texcoord;

uniform mat4 projection;
uniform mat4 modelview;

void main () {

	gl_Position = projection * modelview * vec4(position, 1.0);

}