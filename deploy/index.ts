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
        replicas: 3,
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
                    image: "pulumiaksdemo.azurecr.io/protocom:2.0.2",
                    imagePullPolicy: "Always",
                    lifecycle: {
                        preStop: {
                            exec: {
                                command: ["/bin/sh", "-c", "sleep 20"]
                            }
                        }
                    },
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

export {
    kubeConfig
};
