import {
  Button,
  Center,
  Modal,
  ModalProps,
  Stack,
  Text,
  Textarea,
  TextInput,
  Title,
  useMantineTheme,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiCloseCircle } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { Trans, useTranslation } from 'react-i18next'
import { showErrorNotification } from '@Utils/ApiHelper'
import api, { TeamUpdateModel } from '@Api'

interface TeamEditModalProps extends ModalProps {
  disallowCreate: boolean
  mutate: () => void
}

export const TeamCreateModal: FC<TeamEditModalProps> = (props) => {
  const { disallowCreate, mutate, ...modalProps } = props
  const [createTeam, setCreateTeam] = useState<TeamUpdateModel>({ name: '', bio: '' })
  const [disabled, setDisabled] = useState(false)
  const theme = useMantineTheme()

  const { t } = useTranslation()

  const onCreateTeam = async () => {
    setDisabled(true)

    try {
      const res = await api.team.teamCreateTeam(createTeam)
      showNotification({
        color: 'teal',
        title: t('team.notification.create.success.title'),
        message: t('team.notification.create.success.message', { team: res.data.name }),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      setCreateTeam({ name: '', bio: '' })
      mutate()
      modalProps.onClose()
    } catch (e) {
      showErrorNotification(e, t)
    } finally {
      setDisabled(false)
    }
  }

  return (
    <Modal {...modalProps}>
      {disallowCreate ? (
        <Stack gap="lg" p={40} ta="center">
          <Center>
            <Icon color={theme.colors.red[7]} path={mdiCloseCircle} size={4} />
          </Center>
          <Title order={3}>{t('team.content.disallow_create.title')}</Title>
          <Text>
            <Trans i18nKey="team.content.disallow_create.content" />
          </Text>
        </Stack>
      ) : (
        <Stack>
          <Text>{t('team.content.create')}</Text>
          <TextInput
            label={t('team.label.name')}
            type="text"
            placeholder="team"
            w="100%"
            disabled={disabled}
            value={createTeam?.name ?? ''}
            onChange={(event) => setCreateTeam({ ...createTeam, name: event.currentTarget.value })}
          />
          <Textarea
            label={t('team.label.bio')}
            placeholder={createTeam?.bio ?? t('team.placeholder.bio')}
            value={createTeam?.bio ?? ''}
            w="100%"
            autosize
            minRows={2}
            maxRows={4}
            disabled={disabled}
            onChange={(event) => setCreateTeam({ ...createTeam, bio: event.currentTarget.value })}
          />
          <Button fullWidth variant="outline" onClick={onCreateTeam} disabled={disabled}>
            {t('team.button.create')}
          </Button>
        </Stack>
      )}
    </Modal>
  )
}
