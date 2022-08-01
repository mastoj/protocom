import * as pulumi from "@pulumi/pulumi";
import { Provider } from "@pulumi/kubernetes";
import { StackReference } from "@pulumi/pulumi";
import { Namespace, Service, ServiceAccount } from "@pulumi/kubernetes/core/v1";
import { Role, RoleBinding } from "@pulumi/kubernetes/rbac/v1";
import { Deployment } from "@pulumi/kubernetes/apps/v1";

const infraStack = new StackReference("tomasja/pulumiaksexperiments/dev");
const kubeConfig = infraStack.requireOutput("kubeConfig");

const provider = new Provider("k8s", {
    kubeconfig: kubeConfig,
});

const options = { provider: provider };

const releaseName = "protocom";

const namespace = new Namespace("namespace", {
    metadata: {
        name: releaseName,
    }
}, options)

const serviceAccount = new ServiceAccount("sa", {
    metadata: {
        name: releaseName,
        namespace: releaseName,
    }
}, options)

const role = new Role("role", {
    metadata: {
        name: releaseName,
        namespace: releaseName,
    },
    rules: [
        {
            apiGroups: [""],
            verbs: ["get", "list", "watch", "patch"],
            resources: ["pods"],
        }
    ]
}, options);

const roleBinding = new RoleBinding("rolebinding", {
    metadata: {
        name: releaseName,
        namespace: releaseName,
    },
    roleRef: {
        apiGroup: "rbac.authorization.k8s.io",
        kind: "Role",
        name: releaseName,
    },
    subjects: [
        { 
            kind: "ServiceAccount",
            name: releaseName,
        }
    ]
}, options)

const deploy = new Deployment("deploy", {
    metadata: {
        name: releaseName,
        namespace: releaseName,
        labels: {
            app: releaseName,
        }
    },
    spec: {
        replicas: 5,
        selector: {
            matchLabels: {
                app: releaseName,
            }
        },
        template:  {
            metadata: {
                labels: {
                    app: releaseName,
                },
            },
            spec: {
                serviceAccountName: releaseName,
                securityContext: {
                    runAsUser: 101
                },
                containers: [{
                    name: releaseName,
                    image: "pulumiaksdemo.azurecr.io/protocom:1.0.0",
                    imagePullPolicy: "Always",
                    env: [
                        {
                            name: "ASPNETCORE_URLS",
                            value: "http://*:5000"
                        },
                        {
                            name: "ProtoActor__AdvertisedHost",
                            valueFrom: {
                                fieldRef: {
                                    fieldPath: "status.podIP",
                                }
                            }
                        }
                    ],
                    ports: [
                        {
                            name: "http",
                            containerPort: 5000,
                            protocol: "TCP",
                        }
                    ]

                }]
            }
        }
    }
}, options)

const service = new Service("service", {
    metadata: {
        name: releaseName,
        namespace: releaseName,
    },
    spec: {
        type: "LoadBalancer",
        ports: [
            {
                port: 5000,
                targetPort: "http",
                protocol: "TCP",
                name: "http",                
            }
        ],
        selector: {
            app: releaseName,
        }
    }
}, options);

// // Service
// apiVersion: v1
// kind: Service
// metadata:
//   name: {{ .Release.Name }}
// spec:
//   type: {{ .Values.member.service.type }}
//   ports:
//     - port: {{ .Values.member.service.port }}
//       targetPort: http
//       protocol: TCP
//       name: http
//   selector:
//     app: {{ .Release.Name }}

// // Deployment
// apiVersion: apps/v1
// kind: Deployment
// metadata:
//   name: {{ .Release.Name  }}
//   labels:
//     app: {{ .Release.Name }}
// spec:
//   replicas: {{ .Values.member.replicaCount }}
//   selector:
//     matchLabels:
//       app: {{ .Release.Name }}
//   template:
//     metadata:
//       {{- with .Values.member.podAnnotations }}
//       annotations:
//         {{- toYaml . | nindent 8 }}
//       {{- end }}
//       labels:
//         app: {{ .Release.Name }}
//     spec:
//       serviceAccountName: {{ .Release.Name }}
//       securityContext:
//         {{- toYaml .Values.member.podSecurityContext | nindent 8 }}
//       containers:
//         - name: member
//           securityContext:
//             {{- toYaml .Values.member.securityContext | nindent 12 }}
//           image: "{{ .Values.member.image.repository }}:{{ .Values.member.image.tag }}"
//           imagePullPolicy: {{ .Values.member.image.pullPolicy }}
//           env:
//             - name: ASPNETCORE_URLS
//               value: http://*:5000
//             - name: ProtoActor__AdvertisedHost
//               valueFrom:
//                 fieldRef:
//                   fieldPath: status.podIP
//           ports:
//             - name: http
//               containerPort: 5000
//               protocol: TCP



// // Role binding
// apiVersion: rbac.authorization.k8s.io/v1
// kind: RoleBinding
// metadata:
//   name: {{ .Release.Name }}
// roleRef:
//   apiGroup: rbac.authorization.k8s.io
//   kind: Role
//   name: {{ .Release.Name }}
// subjects:
//   - kind: ServiceAccount
//     name: {{ .Release.Name }}

// apiVersion: rbac.authorization.k8s.io/v1
// kind: Role
// metadata:
//   name: {{ .Release.Name }}
// rules:
//   - apiGroups:
//       - ""
//     resources:
//       - pods
//     verbs:
//       - get
//       - list
//       - watch
//       - patch


// // Service account
// apiVersion: v1
// kind: ServiceAccount
// metadata:
//   name: {{ .Release.Name }}

export {
    kubeConfig
};

// // Deployment
// apiVersion: apps/v1
// kind: Deployment
// metadata:
//   name: {{ .Release.Name  }}
//   labels:
//     app: {{ .Release.Name }}
// spec:
//   replicas: {{ .Values.member.replicaCount }}
//   selector:
//     matchLabels:
//       app: {{ .Release.Name }}
//   template:
//     metadata:
//       {{- with .Values.member.podAnnotations }}
//       annotations:
//         {{- toYaml . | nindent 8 }}
//       {{- end }}
//       labels:
//         app: {{ .Release.Name }}
//     spec:
//       serviceAccountName: {{ .Release.Name }}
//       securityContext:
//         {{- toYaml .Values.member.podSecurityContext | nindent 8 }}
//       containers:
//         - name: member
//           securityContext:
//             {{- toYaml .Values.member.securityContext | nindent 12 }}
//           image: "{{ .Values.member.image.repository }}:{{ .Values.member.image.tag }}"
//           imagePullPolicy: {{ .Values.member.image.pullPolicy }}
//           env:
//             - name: ASPNETCORE_URLS
//               value: http://*:5000
//             - name: ProtoActor__AdvertisedHost
//               valueFrom:
//                 fieldRef:
//                   fieldPath: status.podIP
//           ports:
//             - name: http
//               containerPort: 5000
//               protocol: TCP
// // Role
// apiVersion: rbac.authorization.k8s.io/v1
// kind: Role
// metadata:
//   name: {{ .Release.Name }}
// rules:
//   - apiGroups:
//       - ""
//     resources:
//       - pods
//     verbs:
//       - get
//       - list
//       - watch
//       - patch

// // Role binding
// apiVersion: rbac.authorization.k8s.io/v1
// kind: RoleBinding
// metadata:
//   name: {{ .Release.Name }}
// roleRef:
//   apiGroup: rbac.authorization.k8s.io
//   kind: Role
//   name: {{ .Release.Name }}
// subjects:
//   - kind: ServiceAccount
//     name: {{ .Release.Name }}

// // Service account
// apiVersion: v1
// kind: ServiceAccount
// metadata:
//   name: {{ .Release.Name }}

