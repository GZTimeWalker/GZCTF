import { Box, Button, Group, Stack, Tabs, Text, Title } from '@mantine/core'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { Role } from '@Api'
import { CategoriesPanel } from '@Components/admin/workspace/CategoriesPanel'
import { ChallengesPanel } from '@Components/admin/workspace/ChallengesPanel'
import { MembersPanel } from '@Components/admin/workspace/MembersPanel'
import { SkillTreesPanel } from '@Components/admin/workspace/SkillTreesPanel'
import classes from '@Components/admin/workspace/AdminWorkspace.module.css'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { usePageTitle } from '@Hooks/usePageTitle'

type AdminTab = 'trees' | 'categories' | 'challenges' | 'members'

const isKnownTab = (value: string | null): value is AdminTab =>
  value === 'trees' || value === 'categories' || value === 'challenges' || value === 'members'

const AdminWorkspace = () => {
  const { t } = useTranslation('skillTrees')
  const [searchParams, setSearchParams] = useSearchParams()
  const requested = searchParams.get('tab')
  const tab: AdminTab = isKnownTab(requested) ? requested : 'trees'
  const [createOpen, setCreateOpen] = useState(false)

  const tabLabels: Record<AdminTab, string> = {
    trees: t('list.title'),
    categories: t('category.title'),
    challenges: t('workspace.tabs.challenges'),
    members: t('workspace.tabs.members'),
  }
  usePageTitle(tabLabels[tab])

  const switchTab = (value: string | null) => {
    if (!value) return
    setCreateOpen(false)
    setSearchParams(value === 'trees' ? {} : { tab: value }, { replace: true })
  }

  const openCreate = () => setCreateOpen(true)
  const closeCreate = () => setCreateOpen(false)

  const createLabels: Record<AdminTab, string> = {
    trees: t('list.createTitle'),
    categories: t('category.createTitle'),
    challenges: t('workspace.createChallenge'),
    members: t('workspace.createCohort'),
  }

  return (
    <WithRole requiredRole={Role.Admin}>
      <WithNavBar minWidth={0}>
        <Stack gap="md" className={classes.workspace}>
          <Stack gap={4}>
            <Title order={1}>{t('workspace.title')}</Title>
            <Text c="dimmed" size="sm">
              {t('workspace.subtitle')}
            </Text>
          </Stack>

          <Group justify="space-between" align="flex-start" gap="md" wrap="wrap" className={classes.toolbar}>
            <Tabs value={tab} onChange={switchTab} className={classes.tabs}>
              <Tabs.List className={classes.tabList}>
                <Tabs.Tab value="trees">{tabLabels.trees}</Tabs.Tab>
                <Tabs.Tab value="categories">{tabLabels.categories}</Tabs.Tab>
                <Tabs.Tab value="challenges">{tabLabels.challenges}</Tabs.Tab>
                <Tabs.Tab value="members">{tabLabels.members}</Tabs.Tab>
              </Tabs.List>
            </Tabs>
            <Button className={classes.primaryAction} onClick={openCreate}>
              {createLabels[tab]}
            </Button>
          </Group>

          <Box className={classes.content}>
            {tab === 'trees' && (
              <SkillTreesPanel createOpen={createOpen} onClose={closeCreate} onOpen={openCreate} />
            )}
            {tab === 'categories' && (
              <CategoriesPanel createOpen={createOpen} onClose={closeCreate} onOpen={openCreate} />
            )}
            {tab === 'challenges' && (
              <ChallengesPanel createOpen={createOpen} onClose={closeCreate} onOpen={openCreate} />
            )}
            {tab === 'members' && (
              <MembersPanel createOpen={createOpen} onClose={closeCreate} onOpen={openCreate} />
            )}
          </Box>
        </Stack>
      </WithNavBar>
    </WithRole>
  )
}

export default AdminWorkspace
