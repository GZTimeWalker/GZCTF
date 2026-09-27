import { Center, Group, Loader, Stack, Text } from '@mantine/core'
import { useParams } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useSkillTree } from '@Hooks/useSkillTrees'
import { SkillTreeEnrollmentControls } from '@Components/skill-trees/SkillTreeEnrollmentControls'
import { SkillTreeOutline } from '@Components/skill-trees/SkillTreeOutline'
import { WithNavBar } from '@Components/WithNavbar'
import { usePageTitle } from '@Hooks/usePageTitle'

const SkillTreeDetail = () => {
  const { id } = useParams()
  const { t } = useTranslation('skillTrees')
  const { data: tree, error } = useSkillTree(id)
  usePageTitle(tree?.name ?? t('list.title'))

  return (
    <WithNavBar minWidth={0}>
      <Stack gap="lg">
        {!tree && !error ? (
          <Center>
            <Loader />
          </Center>
        ) : error ? (
          <Text c="red">{t('errors.generic')}</Text>
        ) : (
          <>
            <Group justify="flex-end">
              <SkillTreeEnrollmentControls treeId={tree!.skillTreeId!} />
            </Group>
            <SkillTreeOutline tree={tree!} />
          </>
        )}
      </Stack>
    </WithNavBar>
  )
}

export default SkillTreeDetail
