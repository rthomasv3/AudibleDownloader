<script setup>
import { ref, onBeforeMount, watch } from "vue";
import { useRouter } from "vue-router";
import { useRoute } from "vue-router";
import { Icon } from "@iconify/vue";
import SettingsDialog from "./components/SettingsDialog.vue";

const router = useRouter();
const route = useRoute();

const isLoggedIn = ref(false);
const deviceName = ref(false);
const settingsVisible = ref(false);

watch(route, async (newRoute) => {
  const loginStatusResult = await galdrInvoke("getLoginStatus");
  isLoggedIn.value = loginStatusResult.isLoggedIn;

  if (isLoggedIn.value) {
    const clientInfo = await galdrInvoke("getClientInfo");
    deviceName.value = clientInfo.deviceName;
  }
});

onBeforeMount(async () => {
  const loginStatusResult = await galdrInvoke("getLoginStatus");
  isLoggedIn.value = loginStatusResult.isLoggedIn;

  if (isLoggedIn.value) {
    const clientInfo = await galdrInvoke("getClientInfo");
    deviceName.value = clientInfo.deviceName;
  } else {
    if (!window.location.href.includes("/loading")) {
      await router.push("/login");
    }
  }
});

function onLogout() {
  isLoggedIn.value = false;
}
</script>

<template>
  <div class="layout">
    <div v-if="isLoggedIn" class="layout-topbar shadow-md">
      <div class="layout-topbar-inner flex justify-between items-center">
        <p>{{ deviceName }}</p>
        <div class="flex gap-4 items-center">
          <Button size="small" severity="secondary" variant="text" class="!p-0" rounded @click="settingsVisible = true">
            <template #icon>
              <Icon icon="material-symbols:settings-outline-rounded" width="18" height="18" />
            </template>
          </Button>
        </div>
      </div>
    </div>

    <div class="layout-content">
      <RouterView />
    </div>

    <SettingsDialog v-model:visible="settingsVisible" @logout="onLogout" />

    <!-- <footer class="py-12 footer-container">
    </footer> -->

    <ScrollTop />
  </div>
</template>

<style scoped>
.layout {
  display: flex;
  flex-direction: column;
  min-width: 0;
  width: 100%;
  background-color: var(--body-bg);
}

.layout-topbar {
  background-color: var(--nav-background);
  position: sticky;
  top: 0;
  z-index: 1000;
  opacity: 0.98;
}

.layout-topbar-inner {
  padding: 0.25rem 1.25rem 0.25rem 1rem;
}

.layout-content {
  overflow-y: auto;
  overflow-x: hidden;
  min-width: 0;
}

.footer-container {
  border-top-width: 1px;
  border-color: var(--border-color);
  background-color: var(--banner-background-alt);
}

@media screen and (min-width: 1920px) {

  .layout-footer,
  .layout-topbar-inner {
    margin: 0 auto;
  }
}
</style>
