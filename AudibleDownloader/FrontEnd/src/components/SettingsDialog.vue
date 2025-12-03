<script setup>
import { ref } from "vue";

const visible = defineModel("visible");

</script>

<template>
    <Dialog v-model:visible="visible" modal header="Settings" :style="{ width: '40rem' }" :dismissableMask="true">

    </Dialog>
</template>

<style scoped></style>