<script setup>
import { ref } from "vue";
import ThemeToggler from "./ThemeToggler.vue";
import { Icon } from "@iconify/vue";
import { useConfirm } from "primevue/useconfirm";
import { useRouter } from "vue-router";

const confirm = useConfirm();
const router = useRouter();

const visible = defineModel("visible");
const emit = defineEmits(["logout", "libraryPathUpdated"]);

const libraryPath = ref();

async function onShown() {
    const settings = await galdrInvoke("getSettings");
    libraryPath.value = settings.libraryPath;
}

async function browseDirectory() {
    const result = await galdrInvoke("browseDirectory", { defaultPath: libraryPath.value });
    if (result && result.selectedDirectory) {
        libraryPath.value = result.selectedDirectory;
        await galdrInvoke("updateLibraryPath", { newPath: libraryPath.value });
        emit("libraryPathUpdated", libraryPath.value);
    }
}

function confirmLogout(event) {
    confirm.require({
        target: event.currentTarget,
        message: 'This will log you out and unregister this device',
        rejectProps: {
            label: 'Cancel',
            severity: 'secondary',
            outlined: true
        },
        acceptProps: {
            label: 'Logout',
            severity: 'danger',
        },
        accept: async () => {
            const logoutResult = await galdrInvoke("logoutAndUnregister");
            if (logoutResult && logoutResult.success) {
                visible.value = false;
                await router.push("/login");
                emit("logout");
            }
        },
    });
}
</script>

<template>
    <Dialog v-model:visible="visible" modal header="Settings" :style="{ width: '40rem' }" :dismissableMask="true"
        @show="onShown" :draggable="false">
        <div class="flex flex-col gap-5">
            <div class="flex flex-col gap-2">
                <div class="flex items-center justify-between gap-2">
                    <p class="w-44">Theme</p>
                    <ThemeToggler />
                </div>

                <div class="flex items-center gap-2">
                    <p class="w-44">Library Directory</p>
                    <InputText v-model="libraryPath" size="small" fluid readonly="true" />
                    <Button severity="secondary" size="small" variant="outlined" class="!p-0 h-[2.188rem] !w-15"
                        @click="browseDirectory">
                        <template #icon>
                            <Icon icon="material-symbols:folder-outline-rounded" width="19" height="19" />
                        </template>
                    </Button>
                </div>

            </div>

            <div class="flex justify-between gap-2">
                <ConfirmPopup></ConfirmPopup>
                <Button class="w-24" type="button" label="Logout" severity="danger" @click="confirmLogout"></Button>
                <Button class="w-24" type="button" label="Close" @click="visible = false"></Button>
            </div>
        </div>
    </Dialog>
</template>

<style scoped></style>