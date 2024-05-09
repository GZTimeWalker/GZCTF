import {
  Avatar,
  Box,
  Button,
  Center,
  Divider,
  Grid,
  Group,
  Image,
  Modal,
  Paper,
  SimpleGrid,
  Stack,
  Text,
  Textarea,
  TextInput,
  Title,
} from '@mantine/core'
import { Dropzone, IMAGE_MIME_TYPE } from '@mantine/dropzone'
import { notifications, showNotification, updateNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { Trans, useTranslation } from 'react-i18next'
import PasswordChangeModal from '@Components/PasswordChangeModal'
import WithNavBar from '@Components/WithNavbar'
import { showErrorNotification, tryGetErrorMsg } from '@Utils/ApiHelper'
import { useIsMobile } from '@Utils/ThemeOverride'
import { usePageTitle } from '@Utils/usePageTitle'
import { useUser } from '@Utils/useUser'
import api, { ProfileUpdateModel } from '@Api'

const Profile: FC = () => {
  const [dropzoneOpened, setDropzoneOpened] = useState(false)
  const { user, mutate } = useUser()

  const [profile, setProfile] = useState<ProfileUpdateModel>({
    userName: user?.userName,
    bio: user?.bio,
    stdNumber: user?.stdNumber,
    phone: user?.phone,
    realName: user?.realName,
  })
  const [avatarFile, setAvatarFile] = useState<File | null>(null)

  const [disabled, setDisabled] = useState(false)

  const [mailEditOpened, setMailEditOpened] = useState(false)
  const [pwdChangeOpened, setPwdChangeOpened] = useState(false)

  const [email, setEmail] = useState('')

  const isMobile = useIsMobile()

  const { t } = useTranslation()

  usePageTitle(t('account.title.profile'))

  useEffect(() => {
    setProfile({
      userName: user?.userName,
      bio: user?.bio,
      stdNumber: user?.stdNumber,
      phone: user?.phone,
      realName: user?.realName,
    })
  }, [user])

  const onChangeAvatar = () => {
    if (!avatarFile) return

    setDisabled(true)
    notifications.clean()
    showNotification({
      id: 'upload-avatar',
      color: 'orange',
      message: t('common.avatar.uploading'),
      loading: true,
      autoClose: false,
    })

    api.account
      .accountAvatar({
        file: avatarFile,
      })
      .then(() => {
        updateNotification({
          id: 'upload-avatar',
          color: 'teal',
          message: t('common.avatar.uploaded'),
          icon: <Icon path={mdiCheck} size={1} />,
          autoClose: true,
          loading: false,
        })
        setDisabled(false)
        mutate()
        setAvatarFile(null)
      })
      .catch((err) => {
        updateNotification({
          id: 'upload-avatar',
          color: 'red',
          title: t('common.avatar.upload_failed'),
          message: tryGetErrorMsg(err, t),
          icon: <Icon path={mdiClose} size={1} />,
          autoClose: true,
          loading: false,
        })
      })
      .finally(() => {
        setDisabled(false)
        setDropzoneOpened(false)
      })
  }

  const onChangeProfile = () => {
    api.account
      .accountUpdate(profile)
      .then(() => {
        showNotification({
          color: 'teal',
          message: t('account.notification.profile.profile_updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        mutate({ ...user })
      })
      .catch((e) => showErrorNotification(e, t))
  }

  const onChangeEmail = () => {
    if (!email) return

    api.account
      .accountChangeEmail({
        newMail: email,
      })
      .then((res) => {
        if (res.data.data) {
          showNotification({
            color: 'teal',
            title: t('common.email.sent.title'),
            message: t('common.email.sent.message'),
            icon: <Icon path={mdiCheck} size={1} />,
          })
        } else {
          mutate({ ...user, email: email })
        }
        setMailEditOpened(false)
      })
      .catch((e) => showErrorNotification(e, t))
  }

  const context = (
    <>
      <Title order={2}>{t('account.title.profile')}</Title>
      <Divider mt="xs" mb="md" />
      <Stack gap="md" m="auto">
        <Group wrap="nowrap">
          <TextInput
            label={t('account.label.username')}
            type="text"
            w="100%"
            value={profile.userName ?? 'ctfer'}
            disabled={disabled}
            onChange={(event) => setProfile({ ...profile, userName: event.target.value })}
          />
          <Center>
            <Avatar
              alt="avatar"
              radius={40}
              size={80}
              src={user?.avatar}
              onClick={() => setDropzoneOpened(true)}
            >
              {user?.userName?.slice(0, 1) ?? 'U'}
            </Avatar>
          </Center>
        </Group>
        <SimpleGrid cols={2}>
          <TextInput
            label={t('account.label.email')}
            type="email"
            w="100%"
            value={user?.email ?? 'ctfer@gzti.me'}
            disabled
            readOnly
          />
          <TextInput
            label={t('account.label.phone')}
            type="tel"
            w="100%"
            value={profile.phone ?? ''}
            disabled={disabled}
            onChange={(event) => setProfile({ ...profile, phone: event.target.value })}
          />
          <TextInput
            label={t('account.label.student_id')}
            type="text"
            w="100%"
            value={profile.stdNumber ?? ''}
            disabled={disabled}
            onChange={(event) => setProfile({ ...profile, stdNumber: event.target.value })}
          />
          <TextInput
            label={t('account.label.real_name')}
            type="text"
            w="100%"
            value={profile.realName ?? ''}
            disabled={disabled}
            onChange={(event) => setProfile({ ...profile, realName: event.target.value })}
          />
        </SimpleGrid>
        <Textarea
          label={t('account.label.bio')}
          value={profile.bio ?? t('account.placeholder.bio')}
          w="100%"
          disabled={disabled}
          autosize
          minRows={2}
          maxRows={4}
          onChange={(event) => setProfile({ ...profile, bio: event.target.value })}
        />
        <Box m="auto" w="100%">
          <Grid grow>
            <Grid.Col span={4}>
              <Button
                fullWidth
                color="orange"
                variant="outline"
                disabled={disabled}
                onClick={() => setMailEditOpened(true)}
              >
                {t('account.button.update_email')}
              </Button>
            </Grid.Col>
            <Grid.Col span={4}>
              <Button
                fullWidth
                color="orange"
                variant="outline"
                disabled={disabled}
                onClick={() => setPwdChangeOpened(true)}
              >
                {t('account.button.change_password')}
              </Button>
            </Grid.Col>
            <Grid.Col span={4}>
              <Button fullWidth disabled={disabled} onClick={onChangeProfile}>
                {t('account.button.save_profile')}
              </Button>
            </Grid.Col>
          </Grid>
        </Box>
      </Stack>
    </>
  )

  return (
    <WithNavBar minWidth={0}>
      {isMobile ? (
        <Box mt="md" p="sm">
          {context}
        </Box>
      ) : (
        <Center h="100vh">
          <Paper w="55%" maw={600} shadow="sm" p="5%">
            {context}
          </Paper>
        </Center>
      )}

      <PasswordChangeModal
        opened={pwdChangeOpened}
        onClose={() => setPwdChangeOpened(false)}
        title={t('account.button.change_password')}
      />

      <Modal
        opened={mailEditOpened}
        onClose={() => setMailEditOpened(false)}
        title={t('account.button.update_email')}
      >
        <Stack>
          <Text>
            <Trans i18nKey="account.content.profile.update_email_note"></Trans>
          </Text>
          <TextInput
            required
            label={t('account.label.email_new')}
            type="email"
            w="100%"
            placeholder={user?.email ?? 'ctfer@gzti.me'}
            value={email}
            onChange={(event) => setEmail(event.target.value)}
          />
          <Group justify="right">
            <Button
              variant="default"
              onClick={() => {
                setEmail(user?.email ?? '')
                setMailEditOpened(false)
              }}
            >
              {t('common.modal.cancel')}
            </Button>
            <Button color="orange" onClick={onChangeEmail}>
              {t('common.modal.confirm')}
            </Button>
          </Group>
        </Stack>
      </Modal>

      <Modal
        opened={dropzoneOpened}
        onClose={() => setDropzoneOpened(false)}
        withCloseButton={false}
      >
        <Dropzone
          onDrop={(files) => setAvatarFile(files[0])}
          onReject={() => {
            showNotification({
              color: 'red',
              title: t('common.error.file_invalid.title'),
              message: t('common.error.file_invalid.message'),
              icon: <Icon path={mdiClose} size={1} />,
            })
          }}
          m="0 auto 20px auto"
          miw={220}
          mih={220}
          maxSize={3 * 1024 * 1024}
          accept={IMAGE_MIME_TYPE}
        >
          <Group justify="center" gap="xl" mih={240} style={{ pointerEvents: 'none' }}>
            {avatarFile ? (
              <Image fit="contain" src={URL.createObjectURL(avatarFile)} alt="avatar" />
            ) : (
              <Box>
                <Text size="xl" inline>
                  {t('common.content.drop_zone.content', {
                    type: t('common.content.drop_zone.type.avatar'),
                  })}
                </Text>
                <Text size="sm" c="dimmed" inline mt={7}>
                  {t('common.content.drop_zone.limit')}
                </Text>
              </Box>
            )}
          </Group>
        </Dropzone>
        <Button fullWidth variant="outline" disabled={disabled} onClick={onChangeAvatar}>
          {t('common.avatar.save')}
        </Button>
      </Modal>
    </WithNavBar>
  )
}

export default Profile
