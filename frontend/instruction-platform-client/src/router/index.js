import { createRouter, createWebHistory } from 'vue-router'
import { refreshCurrentUser } from '../api'
import DashboardView from '../views/DashboardView.vue'
import LoginView from '../views/LoginView.vue'
import EmployeesView from '../views/EmployeesView.vue'
import MaterialsView from '../views/MaterialsView.vue'
import TestImportView from '../views/TestImportView.vue'
import MyTestsView from '../views/MyTestsView.vue'
import TestPassingView from '../views/TestPassingView.vue'
import ReportsView from '../views/ReportsView.vue'

const routes = [
  { path: '/', name: 'dashboard', component: DashboardView, meta: { requiresAuth: true } },
  { path: '/login', name: 'login', component: LoginView },
  { path: '/employees', name: 'employees', component: EmployeesView, meta: { requiresAuth: true, roles: ['Admin'] } },
  { path: '/materials', name: 'materials', component: MaterialsView, meta: { requiresAuth: true } },
  { path: '/tests/import', name: 'test-import', component: TestImportView, meta: { requiresAuth: true, roles: ['Admin', 'Manager'] } },
  { path: '/my-tests', name: 'my-tests', component: MyTestsView, meta: { requiresAuth: true, roles: ['Employee'] } },
  { path: '/tests/:id/take', name: 'test-passing', component: TestPassingView, meta: { requiresAuth: true, roles: ['Employee'] } },
  { path: '/reports', name: 'reports', component: ReportsView, meta: { requiresAuth: true, roles: ['Admin', 'Manager'] } }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach(async (to) => {
  const user = await refreshCurrentUser().catch(() => null)

  if (to.meta.requiresAuth && !user) {
    return '/login'
  }

  if (to.meta.roles && !to.meta.roles.includes(user?.role)) {
    return '/'
  }

  if (to.name === 'login' && user) {
    return '/'
  }
})

export default router
